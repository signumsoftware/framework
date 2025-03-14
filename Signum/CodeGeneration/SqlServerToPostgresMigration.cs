using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;
using Npgsql;
using NpgsqlTypes;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using Signum.Engine.Sync.SqlServer;
using System.Data;

namespace Signum.CodeGeneration;

public static class SqlServerToPostgresMigration
{
    public static void MigrateToPostgres(string postgresConnectionString)
    {
        if (!(Connector.Current is SqlServerConnector))
            throw new InvalidOperationException($"Current Connector should be SqlServerConnector");

        if (postgresConnectionString.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(postgresConnectionString));

        var postgreeVersion = PostgresVersionDetector.Detect(postgresConnectionString!, null);
        var sb = new SchemaBuilder();
        var postgresConnector = new PostgreSqlConnector(postgresConnectionString!, sb.Schema, postgreeVersion, builder =>
        {
            builder.EnableTransportSecurity();
            builder.EnableArrays();
            builder.EnableLTree();
            builder.EnableRanges();
            builder.EnableUnmappedTypes();
        });

        Console.Write($"Migrating from SQL Server '{Connector.Current.DatabaseName()}' to Postgres '{postgresConnector.DatabaseName()}'..");

        var tables = SysTablesSchema.GetDatabaseDescription(Schema.Current.DatabaseNames()).Values.ToList()
            .OrderBy(a => a.ToString())
            .ToDictionary(a => a, a => GetNewTableName(sb, a));

        Console.WriteLine();

        var sqlsDT = Executor.ExecuteDataTable("""
            SELECT 
                SCHEMA_NAME(o.schema_id) AS SchemaName,
                o.name AS TableName,
                SUM(ps.row_count) AS [RowCount]
            FROM sys.dm_db_partition_stats ps
            JOIN sys.objects o ON ps.object_id = o.object_id
            WHERE o.type = 'U' -- Only user tables
            AND ps.index_id IN (0,1) -- 0 = Heap, 1 = Clustered Index
            GROUP BY o.schema_id, o.name
            ORDER BY SCHEMA_NAME(o.schema_id), o.name;
            """);

        var sqlStats = sqlsDT.Rows.Cast<DataRow>().ToDictionary(a => new ObjectName(new SchemaName(null, (string)a["SchemaName"], true), (string)a["TableName"], true),
                a => (long)a["RowCount"]);


        DiffTable? continueOnTable = null;

        using (Connector.Override(postgresConnector))
        {
            if (postgresConnector.HasTables())
            {
                var pgDT = Executor.ExecuteDataTable("""
                    SELECT schemaname, relname AS table_name, n_live_tup AS row_count
                    FROM pg_stat_user_tables
                    ORDER BY schemaname, relname;
                    """);

                var pgStats = pgDT.Rows.Cast<DataRow>().ToDictionary(a => new ObjectName(new SchemaName(null, (string)a["schemaname"], true), (string)a["table_name"], true),
                    a => (long)a["row_count"]);


                if (tables.Any(kvp => !sqlStats.ContainsKey(kvp.Key.Name)))
                    throw new InvalidOperationException("unexpected");

                if (tables.Any(kvp => !pgStats.ContainsKey(kvp.Value)))
                    SafeConsole.WriteLineColor(ConsoleColor.Yellow, "Target database has missing tables");
                else
                {
                    Console.WriteLine("Recovering previous migration...");
                    continueOnTable = tables.FirstOrDefault(kvp =>
                    {
                        var sqls = sqlStats[kvp.Key.Name];
                        var pg = pgStats[kvp.Value];

                        SafeConsole.WriteLineColor(sqls == pg ? ConsoleColor.Green : ConsoleColor.Yellow, $"{kvp.Key.Name} (ROWS = {sqls}) {kvp.Value} (ROWS = {pg}) {(sqls == pg ? "OK" : "ERROR")}");

                        return sqls != pg;
                    }).Key;

                    if (continueOnTable == null)
                    {
                        SafeConsole.WriteLineColor(ConsoleColor.Green, "Already copied!");
                        return;
                    }
                }
            }
        }


        if (continueOnTable != null && SafeConsole.Ask("Continue on table " + continueOnTable + "?"))
        {
            var toRemove = tables.Keys.TakeWhile(a => a != continueOnTable).ToList();
            tables.RemoveRange(toRemove);
            using (Connector.Override(postgresConnector))
            {
                Console.Write("Cleaning " + continueOnTable + "...");
                var num = Executor.ExecuteNonQuery($"DELETE FROM {tables[continueOnTable]}");
                Console.WriteLine(num + " rows deleted");
            }
        }
        else
        {
            if (continueOnTable != null)
            {
                var anwer = SafeConsole.AskString($"Clean Postgres database '{postgresConnector.DatabaseName()}'? (write the name)",
                    db => db == "" || db == postgresConnector.DatabaseName() ? null : "Wrong name");

                if (!anwer.HasText())
                    return;

                using (Connector.Override(postgresConnector))
                    postgresConnector.CleanDatabase(null);
            }

            Console.Write("Creating new database...");
            var script = CreatePostgresTables(tables, sb!);
            using (Connector.Override(postgresConnector))
                script.ExecuteLeaves();
            Console.WriteLine("Done!");

            if (!SafeConsole.Ask("Continue copying tables?"))
                return;

            postgresConnector.ChangeConnectionStringDatabase(postgresConnector.DatabaseName()); //Reload LTree
        }


        foreach (var (diffTable, pgName) in tables)
        {
            CopyTable(diffTable, pgName, postgresConnector, sb, batchSize: 10000);
        }
    }

    public static SqlPreCommand CreatePostgresTables(Dictionary<DiffTable, ObjectName> tables, SchemaBuilder sb)
    {
        List<SqlPreCommandSimple> result = new List<SqlPreCommandSimple>();
        HashSet<SchemaName> createdSchemas = new HashSet<SchemaName>(); // Track created schemas

        if (tables.Keys.Any(a => a.Columns.Any(c => c.Value.UserTypeName == "hierarchyid")))
        {
            result.Add(new SqlPreCommandSimple("CREATE EXTENSION IF NOT EXISTS ltree;"));
        }

        foreach (var (diffTable, pgName) in tables)
        {
            // Ensure schema exists in PostgreSQL
            if (!pgName.Schema.IsDefault() && !createdSchemas.Contains(pgName.Schema))
            {
                result.Add(new SqlPreCommandSimple($"CREATE SCHEMA IF NOT EXISTS \"{pgName.Schema}\";"));
            }

            result.Add(CreateTable(diffTable, pgName, sb));
        }

        return result.Combine(Spacing.Simple)!;
    }

    private static ObjectName GetNewTableName(SchemaBuilder sb, DiffTable table)
    {
        var schema = table.Name.Schema.IsDefault() ? SchemaName.Default(isPostgres: true) : new SchemaName(null, sb.Idiomatic(table.Name.Schema.Name), isPostgres: true);
        var newName = new ObjectName(schema, sb.Idiomatic(table.Name.Name), isPostgres: true);
        return newName;
    }


    public static Func<string, bool> IsSysStartDate = a => a == "SysStartDate";
    public static Func<string, bool> IsSysEndDate = a => a == "SysEndDate";

    private static SqlPreCommandSimple CreateTable(DiffTable table, ObjectName newName, SchemaBuilder sb)
    {
        return new SqlPreCommandSimple($"""
            CREATE TABLE {newName} (
            {table.Columns.Values.Where(a => !IsSysStartDate(a.Name) && !IsSysEndDate(a.Name))
            .Select(col =>
            $"    {PostgresColumn(col, sb)}"
            ).ToString(",\n")}
            {(table.Columns.Values.Any(a => IsSysStartDate(a.Name) ||IsSysEndDate(a.Name))? ",\nsys_period TSTZRANGE NOT NULL" : null)}
            );
            """);
    }

    private static string PostgresColumn(DiffColumn col, SchemaBuilder sb)
    {
        string result = sb.Idiomatic(col.Name).SqlEscape(isPostgres: true);
        var pgType = col.UserTypeName.HasText() && col.UserTypeName == "hierarchyid" ? new AbstractDbType(NpgsqlDbType.LTree) :
            col.DbType.SqlServer is SqlDbType.DateTime or SqlDbType.DateTime2 or SqlDbType.DateTime2 ? new AbstractDbType(Schema.Current.TimeZoneMode == TimeZoneMode.Utc ? NpgsqlDbType.TimestampTz: NpgsqlDbType.Timestamp) :
            sb.Settings.TypeValues.Values.Where(a => a.HasPostgres && a.HasSqlServer)
            .FirstEx(a => a.SqlServer == col.DbType.SqlServer);

        result += " " + pgType.ToString(isPostgres: true);

        if (IsString(col.DbType.SqlServer) && col.Length != 0 && col.Length != -1)
            result += $"({col.Length})";

        else if (col.DbType.SqlServer == SqlDbType.Decimal && col.Precision != 0 && col.Scale != 0)
            result += $"({col.Precision},{col.Scale})";

        if (col.Identity)
            result += " GENERATED ALWAYS AS IDENTITY";

        if (col.PrimaryKey)
            result += " PRIMARY KEY";

        if (!col.Nullable)
            result += " NOT NULL";

        return result;
    }

    private static bool IsString(SqlDbType sqlServer) => sqlServer == SqlDbType.VarChar || sqlServer == SqlDbType.NVarChar;


    private static void BulkInsertToPostgres(PostgreSqlConnector pgConnector, DataTable dataTable,  DiffTable table, SchemaBuilder sb)
    {
        using (var conn = (NpgsqlConnection)pgConnector.CreateConnection())
        {
            conn.Open();

            using (var transaction = conn.BeginTransaction()) // Start transaction
            {
                try
                {
                    var newName = GetNewTableName(sb, table);
                    var pgColumnNames = table.Columns.Values
                        .Where(c=>!IsSysStartDate(c.Name) && !IsSysEndDate(c.Name))
                        .ToDictionary(c => sb.Idiomatic(c.Name).SqlEscape(isPostgres: true));

                    var sysStart = table.Columns.Values.SingleOrDefault(c => IsSysStartDate(c.Name));
                    var sysEnd = table.Columns.Values.SingleOrDefault(c => IsSysEndDate(c.Name));

                    if (sysStart != null && sysEnd != null)
                        pgColumnNames.Add("sys_period", null!);

                    using (var writer = conn.BeginBinaryImport($"COPY \"{newName.Schema}\".\"{newName.Name}\" ({pgColumnNames.Keys.ToString(", ")}) FROM STDIN (FORMAT BINARY)"))
                    {
                        foreach (DataRow row in dataTable.Rows)
                        {
                            writer.StartRow();

                            foreach (var (pg, diffCol) in pgColumnNames)
                            {
                                if (pg == "sys_period") // Manually construct tstzrange
                                {
                                    var startDate = row[sysStart!.Name] == DBNull.Value ? null : (DateTime?)row[sysStart!.Name];
                                    var endDate = row[sysEnd!.Name] == DBNull.Value ? null : (DateTime?)row[sysEnd!.Name];
                                    //writer.Write(new NpgsqlRange<DateTime?>(startDate.HasValue ? startDate.Value : null, endDate.HasValue ? endDate.Value : null), NpgsqlDbType.Range | NpgsqlDbType.TimestampTz);
                                    writer.Write(new NpgsqlRange<DateTime>(startDate ?? DateTime.MinValue, endDate ?? DateTime.MaxValue), NpgsqlDbType.Range | NpgsqlDbType.Timestamp);
                                }
                                else
                                {
                                    if(diffCol.DbType.SqlServer == SqlDbType.Time)
                                        writer.Write<TimeOnly?>(row[diffCol.Name] == DBNull.Value ? null : ((TimeSpan)row[diffCol.Name]).ToTimeOnly());
                                    else if (diffCol.DbType.SqlServer == SqlDbType.Date)
                                        writer.Write<DateOnly?>(row[diffCol.Name] == DBNull.Value ? null : ((DateTime)row[diffCol.Name]).ToDateOnly());
                                    else if (diffCol.UserTypeName == "hierarchyid")
                                        writer.Write(row[diffCol.Name] == DBNull.Value ? null : HierarchyIdString.ToSortableString((SqlHierarchyId)row[diffCol.Name]), NpgsqlDbType.LTree);
                                    else
                                        writer.Write(row[diffCol.Name] == DBNull.Value ? null : row[diffCol.Name]);
                                }
                            }
                        }

                        writer.Complete(); // Commit COPY operation
                    }

                    transaction.Commit(); // Commit the transaction

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in bulk insert: {ex.Message}");
                    transaction.Rollback(); // Rollback if any issue occurs
                }
            }
        }
    }


    private static void CopyTable(DiffTable table, ObjectName pgName, PostgreSqlConnector pgConnector, SchemaBuilder sb, int batchSize)
    {

        Console.Write($"Copying {table.Name} ...");

        int rowsCopied = 0;

        try
        {
            while (true)
            {
                DataTable dataTable = SelectBatchSqlServer(table, batchSize, rowsCopied);

                if (dataTable.Rows.Count == 0)
                    break;

                BulkInsertToPostgres(pgConnector, dataTable, table, sb);
                rowsCopied += dataTable.Rows.Count;
                Console.Write(".");

                if (dataTable.Rows.Count < batchSize)
                    break;
            }
        }
        catch (Exception ex)
        {
            SafeConsole.WriteLineColor(ConsoleColor.Red, $"Error copying data for {table} (offset {rowsCopied}): {ex.Message}");
            throw;
        }

        Console.WriteLine($"Done!  ({rowsCopied} rows)");
    }

    private static DataTable SelectBatchSqlServer(DiffTable table, int batchSize, int offset)
    {
        DataTable dataTable = new DataTable();
        var columnNames = table.Columns.Select(c => c.Key).ToList();
        string columnList = columnNames.Select(c => c.SqlEscape(isPostgres: false)).ToString(", ");

        using (var mssqlConnection = (SqlConnection)Connector.Current.CreateConnection())
        {
            mssqlConnection.Open();

            using (SqlCommand command = new SqlCommand(
                $"SELECT {columnList} FROM {table} ORDER BY (SELECT NULL) OFFSET @Offset ROWS FETCH NEXT @BatchSize ROWS ONLY",
                mssqlConnection))
            {
                command.Parameters.AddWithValue("@Offset", offset);
                command.Parameters.AddWithValue("@BatchSize", batchSize);

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }
            }
        }

        return dataTable;
    }
}
