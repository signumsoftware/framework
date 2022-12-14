using System.Data.Common;
using System.Data;
using Signum.Engine.Maps;
using System.Data.SqlTypes;
using Signum.Engine.Connection;
using Signum.Engine.SchemaInfoTables;
using Microsoft.Data.SqlClient;

namespace Signum.Engine;

public enum SqlServerVersion
{
    SqlServer2005,
    SqlServer2008,
    SqlServer2012,
    SqlServer2014,
    SqlServer2016,
    SqlServer2017,
    SqlServer2019,

    AzureSQL,
}

public static class SqlServerVersionDetector
{
    public enum EngineEdition
    {
        Personal = 1,
        Standard = 2,
        Enterprise = 3,
        Express = 4,
        Azure = 5,
    }

    public static SqlServerVersion? Detect(string connectionString)
    {
        return SqlServerRetry.Retry(() =>
        {
            using (var con = new SqlConnection(connectionString))
            {
                con.Open();
                var sql =
@"SELECT
    SERVERPROPERTY ('ProductVersion') as ProductVersion,
    SERVERPROPERTY('ProductLevel') as ProductLevel,
    SERVERPROPERTY('Edition') as Edition,
    SERVERPROPERTY('EngineEdition') as EngineEdition";

                using (var cmd = new SqlCommand(sql, con))
                {
                    var da = new SqlDataAdapter(cmd);

                    var result = new DataTable();
                    da.Fill(result);

                    if ((int)result.Rows[0]["EngineEdition"] == (int)EngineEdition.Azure)
                        return SqlServerVersion.AzureSQL;

                    var version = (string)result.Rows[0]["ProductVersion"];

                    return version.Before(".") switch
                    {
                        "8" => throw new InvalidOperationException("SQL Server 2000 is not supported"),
                        "9" => SqlServerVersion.SqlServer2005,
                        "10" => SqlServerVersion.SqlServer2008,
                        "11" => SqlServerVersion.SqlServer2012,
                        "12" => SqlServerVersion.SqlServer2014,
                        "13" => SqlServerVersion.SqlServer2016,
                        "14" => SqlServerVersion.SqlServer2017,
                        "15" => SqlServerVersion.SqlServer2019,
                        _ => (SqlServerVersion?)null,
                    };
                }
            }
        });
    }
}

public class SqlServerConnector : Connector
{
    public ResetLazy<Tuple<byte>> DateFirstLazy = new ResetLazy<Tuple<byte>>(() => Tuple.Create((byte)Executor.ExecuteScalar("SELECT @@DATEFIRST")!));
    public byte DateFirst => DateFirstLazy.Value.Item1;

    public SqlServerVersion Version { get; set; }

    public SqlServerConnector(string connectionString, Schema schema, SqlServerVersion version) : base(schema)
    {
        this.ConnectionString = connectionString;
        this.ParameterBuilder = new SqlParameterBuilder();

        this.Version = version;
    }

    public int? CommandTimeout { get; set; } = null;

    public string ConnectionString { get; set; }

    public override bool SupportsScalarSubquery { get { return true; } }
    public override bool SupportsScalarSubqueryInAggregates { get { return false; } }

    T EnsureConnectionRetry<T>(Func<SqlConnection?, T> action)
    {
        if (Transaction.HasTransaction)
            return action(null);

        return SqlServerRetry.Retry(() =>
        {
            using (var con = new SqlConnection(this.ConnectionString))
            {
                con.Open();

                return action(con);
            }
        });
    }

    SqlCommand NewCommand(SqlPreCommandSimple preCommand, SqlConnection? overridenConnection, CommandType commandType)
    {
        var cmd = new SqlCommand { CommandType = commandType };

        int? timeout = Connector.ScopeTimeout ?? CommandTimeout;
        if (timeout.HasValue)
            cmd.CommandTimeout = timeout.Value;

        if (overridenConnection != null)
            cmd.Connection = overridenConnection;
        else
        {
            cmd.Connection = (SqlConnection)Transaction.CurrentConnection!;
            cmd.Transaction = (SqlTransaction)Transaction.CurrentTransaction!;
        }

        cmd.CommandText = preCommand.Sql;

        if (preCommand.Parameters != null)
        {
            foreach (SqlParameter param in preCommand.Parameters)
            {
                cmd.Parameters.Add(CloneParameter(param));
            }
        }

        Log(preCommand);

        return cmd;
    }

    protected internal override object? ExecuteScalar(SqlPreCommandSimple preCommand, CommandType commandType)
    {
        return EnsureConnectionRetry(con =>
        {
            using (SqlCommand cmd = NewCommand(preCommand, con, commandType))
            using (HeavyProfiler.Log("SQL", () => preCommand.sp_executesql()))
            {
                try
                {
                    object result = cmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        return null;

                    return result;
                }
                catch (Exception ex)
                {
                    var nex = HandleException(ex, preCommand);
                    if (nex == ex)
                        throw;

                    throw nex;
                }
            }
        });
    }

    protected internal override int ExecuteNonQuery(SqlPreCommandSimple preCommand, CommandType commandType)
    {
        return EnsureConnectionRetry(con =>
        {
            using (SqlCommand cmd = NewCommand(preCommand, con, commandType))
            using (HeavyProfiler.Log("SQL", () => preCommand.sp_executesql()))
            {
                try
                {
                    int result = cmd.ExecuteNonQuery();
                    return result;
                }
                catch (Exception ex)
                {
                    var nex = HandleException(ex, preCommand);
                    if (nex == ex)
                        throw;

                    throw nex;
                }
            }
        });
    }

    public void ExecuteDataReaderDependency(SqlPreCommandSimple preCommand, OnChangeEventHandler change, Action reconect, Action<FieldReader> forEach, CommandType commandType)
    {
        bool reconected = false;
    retry:
        try
        {
            EnsureConnectionRetry(con =>
            {
                using (SqlCommand cmd = NewCommand(preCommand, con, commandType))
                using (HeavyProfiler.Log("SQL-Dependency"))
                using (HeavyProfiler.Log("SQL", () => preCommand.sp_executesql()))
                {
                    try
                    {
                        if (change != null)
                        {
                            var dep = new SqlDependency(cmd);
                            dep.OnChange += change;
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            var fr = new FieldReader(reader);
                            int row = -1;
                            try
                            {
                                while (reader.Read())
                                {
                                    row++;
                                    forEach(fr);
                                }

                                return 0;
                            }
                            catch (Exception ex)
                            {
                                FieldReaderException fieldEx = fr.CreateFieldReaderException(ex);
                                fieldEx.Command = preCommand;
                                fieldEx.Row = row;
                                throw fieldEx;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var nex = HandleException(ex, preCommand);
                        if (nex == ex)
                            throw;

                        throw nex;
                    }
                }
            });
        }
        catch (InvalidOperationException ioe)
        {
            if (ioe.Message.Contains("SqlDependency.Start()") && !reconected)
            {
                reconect();

                reconected = true;

                goto retry;
            }

            throw;
        }
    }

    protected internal override DbDataReaderWithCommand UnsafeExecuteDataReader(SqlPreCommandSimple preCommand, CommandType commandType)
    {
        try
        {
            var cmd = NewCommand(preCommand, null, commandType);

            var reader = cmd.ExecuteReader();

            return new DbDataReaderWithCommand(cmd, reader);
        }
        catch (Exception ex)
        {
            var nex = HandleException(ex, preCommand);
            if (nex == ex)
                throw;

            throw nex;
        }
    }

    protected internal override async Task<DbDataReaderWithCommand> UnsafeExecuteDataReaderAsync(SqlPreCommandSimple preCommand, CommandType commandType, CancellationToken token)
    {
        try
        {
            var cmd = NewCommand(preCommand, null, commandType);

            var reader = await cmd.ExecuteReaderAsync(token);

            return new DbDataReaderWithCommand(cmd, reader);
        }
        catch (Exception ex)
        {
            var nex = HandleException(ex, preCommand);
            if (nex == ex)
                throw;

            throw nex;
        }
    }

    protected internal override DataTable ExecuteDataTable(SqlPreCommandSimple preCommand, CommandType commandType)
    {
        return EnsureConnectionRetry(con =>
        {
            using (SqlCommand cmd = NewCommand(preCommand, con, commandType))
            using (HeavyProfiler.Log("SQL", () => preCommand.sp_executesql()))
            {
                try
                {
                    var da = new SqlDataAdapter(cmd);

                    var result = new DataTable();
                    da.Fill(result);
                    return result;
                }
                catch (Exception ex)
                {
                    var nex = HandleException(ex, preCommand);
                    if (nex == ex)
                        throw;

                    throw nex;
                }
            }
        });
    }

    public static Exception HandleException(Exception ex, SqlPreCommandSimple command)
    {
        var nex = ReplaceException(ex, command);
        nex.Data["Sql"] = command.sp_executesql();
        return nex;
    }

    static Exception ReplaceException(Exception ex, SqlPreCommandSimple command)
    {
        if (ex is SqlException se)
        {
            return se.Number switch
            {
                -2 => new TimeoutException(ex.Message, ex),
                2601 => new UniqueKeyException(ex),
                547 => new ForeignKeyException(ex),
                _ => ex,
            };
        }

        if (ex is SqlTypeException ste && ex.Message.Contains("DateTime"))
        {
            var mins = command.Parameters!.Where(a => DateTime.MinValue.Equals(a.Value));

            if (mins.Any())
            {
                return new ArgumentOutOfRangeException("{0} {1} not initialized and equal to DateTime.MinValue".FormatWith(
                    mins.CommaAnd(a => a.ParameterName),
                    mins.Count() == 1 ? "is" : "are"), ex);
            }
        }

        return ex;
    }

    protected internal override void BulkCopy(DataTable dt, List<IColumn> columns, ObjectName destinationTable, SqlBulkCopyOptions options, int? timeout)
    {
        EnsureConnectionRetry(con =>
        {
            using (var bulkCopy = new SqlBulkCopy(
                options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction) ? con : (SqlConnection)Transaction.CurrentConnection!,
                options,
                options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction) ? null : (SqlTransaction)Transaction.CurrentTransaction!))
            using (HeavyProfiler.Log("SQL", () => destinationTable.ToString() + " Rows:" + dt.Rows.Count))
            {
                bulkCopy.BulkCopyTimeout = timeout ?? Connector.ScopeTimeout ?? this.CommandTimeout ?? bulkCopy.BulkCopyTimeout;

                foreach (var c in dt.Columns.Cast<DataColumn>())
                    bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(c.ColumnName, c.ColumnName));

                bulkCopy.DestinationTableName = destinationTable.ToString();
                bulkCopy.WriteToServer(dt);
                return 0;
            }
        });
    }

    public override string DatabaseName()
    {
        return new SqlConnection(ConnectionString).Database;
    }

    public override string DataSourceName()
    {
        return new SqlConnection(ConnectionString).DataSource;
    }

    public override void SaveTransactionPoint(DbTransaction transaction, string savePointName)
    {
        ((SqlTransaction)transaction).Save(savePointName);
    }

    public override void RollbackTransactionPoint(DbTransaction transaction, string savePointName)
    {
        ((SqlTransaction)transaction).Rollback(savePointName);
    }

    public override string GetSqlDbType(DbParameter p)
    {
        return ((SqlParameter)p).SqlDbType.ToString().ToUpperInvariant();
    }

    public override DbParameter CloneParameter(DbParameter p)
    {
        return (SqlParameter)((ICloneable)p).Clone();
    }

    public override DbConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    public override ParameterBuilder ParameterBuilder { get; protected set; }

    public override void CleanDatabase(DatabaseName? database)
    {
        SqlConnectorScripts.RemoveAllScript(database).ExecuteLeaves();
        ShrinkDatabase(database?.ToString() ?? DatabaseName());
    }

    public override bool AllowsMultipleQueries
    {
        get { return true; }
    }

    public override Connector ForDatabase(Maps.DatabaseName? database)
    {
        if (database == null)
            return this;

        return new SqlServerConnector(Replace(ConnectionString, database), this.Schema, this.Version);
    }

    private static string Replace(string connectionString, DatabaseName item)
    {
        var csb = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = item.Name
        };
        return csb.ToString();
    }

    public override bool AllowsSetSnapshotIsolation => this.Version >= SqlServerVersion.SqlServer2008;

    public override bool AllowsIndexWithWhere(string Where)
    {
        return Version > SqlServerVersion.SqlServer2005 && !ComplexWhereKeywords.Any(Where.Contains);
    }

    public override bool RequiresRetry => this.Version == SqlServerVersion.AzureSQL;

    public override bool SupportsDateDifBig => this.Version >= SqlServerVersion.SqlServer2016;

    public static List<string> ComplexWhereKeywords = new() { "OR" };

    public SqlPreCommand ShrinkDatabase(string databaseName)
    {
        return new[]
        {
                this.Version == SqlServerVersion.SqlServer2005 ?
                    new SqlPreCommandSimple("BACKUP LOG {0} WITH TRUNCATE_ONLY".FormatWith(databaseName)):
                    new []
                    {
                        new SqlPreCommandSimple("ALTER DATABASE {0} SET RECOVERY SIMPLE WITH NO_WAIT".FormatWith(databaseName)),
                        new[]{
                            new SqlPreCommandSimple("DECLARE @fileID BIGINT"),
                            new SqlPreCommandSimple("SET @fileID = (SELECT FILE_IDEX((SELECT TOP(1)name FROM sys.database_files WHERE type = 1)))"),
                            new SqlPreCommandSimple("DBCC SHRINKFILE(@fileID, 1)"),
                        }.Combine(Spacing.Simple)!.PlainSqlCommand(),
                        new SqlPreCommandSimple("ALTER DATABASE {0} SET RECOVERY FULL WITH NO_WAIT".FormatWith(databaseName)),
                    }.Combine(Spacing.Simple),
                new SqlPreCommandSimple("DBCC SHRINKDATABASE ( {0} , TRUNCATEONLY )".FormatWith(databaseName))
            }.Combine(Spacing.Simple)!;
    }

    public override bool AllowsConvertToDate
    {
        get { return Version >= SqlServerVersion.SqlServer2008; }
    }

    public override bool AllowsConvertToTime
    {
        get { return Version >= SqlServerVersion.SqlServer2008; }
    }

    public override bool SupportsSqlDependency
    {
        get { return Version != SqlServerVersion.AzureSQL && Version >= SqlServerVersion.SqlServer2008; }
    }

    public override bool SupportsFormat
    {
        get { return Version >= SqlServerVersion.SqlServer2012; }
    }

    public override bool SupportsTemporalTables
    {
        get { return Version >= SqlServerVersion.SqlServer2016; }
    }

    public override int MaxNameLength => 128;

    public override string ToString() => $"SqlServerConnector({Version}, Database: {this.DatabaseName()}, DataSource: {this.DataSourceName()})";

    public override bool HasTables()
    {
        return Database.View<SysTables>().Any();
    }
}

public class SqlParameterBuilder : ParameterBuilder
{
    public override DbParameter CreateParameter(string parameterName, AbstractDbType dbType, string? udtTypeName, bool nullable, object? value)
    {
        if (dbType.IsDate())
        {
            if (value is DateTime dt)
                AssertDateTime(dt);
            else if (value is DateOnly d)
                value = d.ToDateTime();
        }
        else if (dbType.IsTime())
        {
            if (value is TimeOnly to)
                value = to.ToTimeSpan();
        }

        var result = new SqlParameter(parameterName, value ?? DBNull.Value)
        {
            IsNullable = nullable
        };

        result.SqlDbType = dbType.SqlServer;
        if (udtTypeName != null)
            result.UdtTypeName = udtTypeName;

        return result;
    }

    public override MemberInitExpression ParameterFactory(Expression parameterName, AbstractDbType dbType, int? size, byte? precision, byte? scale, string? udtTypeName, bool nullable, Expression value)
    {
        var uType = value.Type.UnNullify();

        var exp =
            uType == typeof(DateTime) ? Expression.Call(miAsserDateTime, Expression.Convert(value, typeof(DateTime?))) :
            //https://github.com/dotnet/SqlClient/issues/1009
            uType == typeof(DateOnly) ? Expression.Call(miToDateTimeKind, Expression.Convert(value, typeof(DateOnly)), Expression.Constant(Schema.Current.DateTimeKind)) :
            uType == typeof(TimeOnly) ? Expression.Call(Expression.Convert(value, typeof(TimeOnly)), miToTimeSpan) :
            value;

        Expression valueExpr = Expression.Convert(exp, typeof(object));

        if (nullable)
            valueExpr = Expression.Condition(Expression.Equal(value, Expression.Constant(null, value.Type)),
                        Expression.Constant(DBNull.Value, typeof(object)),
                        valueExpr);

        NewExpression newExpr = Expression.New(typeof(SqlParameter).GetConstructor(new[] { typeof(string), typeof(object) })!, parameterName, valueExpr);


        List<MemberBinding> mb = new()
        {
            Expression.Bind(typeof(SqlParameter).GetProperty(nameof(SqlParameter.IsNullable))!, Expression.Constant(nullable)),
            Expression.Bind(typeof(SqlParameter).GetProperty(nameof(SqlParameter.SqlDbType))!, Expression.Constant(dbType.SqlServer)),
        };

        if (size != null)
            mb.Add(Expression.Bind(typeof(SqlParameter).GetProperty(nameof(SqlParameter.Size))!, Expression.Constant(size)));

        if (precision != null)
            mb.Add(Expression.Bind(typeof(SqlParameter).GetProperty(nameof(SqlParameter.Precision))!, Expression.Constant(precision)));

        if (scale != null)
            mb.Add(Expression.Bind(typeof(SqlParameter).GetProperty(nameof(SqlParameter.Scale))!, Expression.Constant(scale)));

        if (udtTypeName != null)
            mb.Add(Expression.Bind(typeof(SqlParameter).GetProperty(nameof(SqlParameter.UdtTypeName))!, Expression.Constant(udtTypeName)));

        return Expression.MemberInit(newExpr, mb);
    }
}

public static class SqlConnectorScripts
{
    public static readonly string RemoveAllConstraintsScript =
@"declare @schema nvarchar(128), @tbl nvarchar(128), @constraint nvarchar(128)
DECLARE @sql nvarchar(255)

declare cur cursor fast_forward for
select distinct cu.constraint_schema, cu.table_name, cu.constraint_name
from information_schema.table_constraints tc
join information_schema.referential_constraints rc on rc.unique_constraint_name = tc.constraint_name
join information_schema.constraint_column_usage cu on cu.constraint_name = rc.constraint_name
open cur
    fetch next from cur into @schema, @tbl, @constraint
    while @@fetch_status <> -1
    begin
        select @sql = 'ALTER TABLE [' + @schema + '].[' + @tbl + '] DROP CONSTRAINT [' + @constraint + '];'
        exec sp_executesql @sql
        fetch next from cur into @schema, @tbl, @constraint
    end
close cur
deallocate cur";

    public static readonly string RemoveAllTablesScript =
@"declare @schema nvarchar(128), @tbl nvarchar(128)
DECLARE @sql nvarchar(255)

declare cur cursor fast_forward for
select distinct table_schema, table_name
from information_schema.tables where table_type = 'BASE TABLE'
open cur
    fetch next from cur into @schema, @tbl
    while @@fetch_status <> -1
    begin
        select @sql = 'DROP TABLE [' + @schema + '].[' + @tbl + '];'
        exec sp_executesql @sql
        fetch next from cur into @schema, @tbl
    end
close cur
deallocate cur";

    public static readonly string RemoveAllViewsScript =
@"declare @schema nvarchar(128), @view nvarchar(128)
DECLARE @sql nvarchar(255)

declare cur cursor fast_forward for
select distinct table_schema, table_name
from information_schema.tables where table_type = 'VIEW'
and table_schema not in ({0})
open cur
    fetch next from cur into @schema, @view
    while @@fetch_status <> -1
    begin
        select @sql = 'DROP VIEW [' + @schema + '].[' + @view + '];'
        exec sp_executesql @sql
        fetch next from cur into @schema, @view
    end
close cur
deallocate cur";

    public static readonly string RemoveAllProceduresScript =
@"declare @schema nvarchar(128), @proc nvarchar(128), @type nvarchar(128)
DECLARE @sql nvarchar(255)

declare cur cursor fast_forward for
select routine_schema, routine_name, routine_type
from information_schema.routines
open cur
    fetch next from cur into @schema, @proc, @type
    while @@fetch_status <> -1
    begin
        select @sql = 'DROP '+ @type +' [' + @schema + '].[' + @proc + '];'
        exec sp_executesql @sql
        fetch next from cur into @schema, @proc, @type
    end
close cur
deallocate cur";

    public static readonly string RemoveAllSchemasScript =
@"declare @schema nvarchar(128)
DECLARE @sql nvarchar(255)

declare cur cursor fast_forward for
select schema_name
from information_schema.schemata
where schema_name not in ({0})
open cur
    fetch next from cur into @schema
    while @@fetch_status <> -1
    begin
        select @sql = 'DROP SCHEMA [' + @schema + '];'
        exec sp_executesql @sql
        fetch next from cur into @schema
    end
close cur
deallocate cur";

    public static readonly string StopSystemVersioning = @"declare @schema nvarchar(128), @tbl nvarchar(128)
DECLARE @sql nvarchar(255)

declare cur cursor fast_forward for
select distinct s.name, t.name
from sys.tables t
join sys.schemas s on t.schema_id = s.schema_id where history_table_id is not null
open cur
    fetch next from cur into @schema, @tbl
    while @@fetch_status <> -1
    begin
        select @sql = 'ALTER TABLE [' + @schema + '].[' + @tbl + '] SET (SYSTEM_VERSIONING = OFF);'
        exec sp_executesql @sql
        fetch next from cur into @schema, @tbl
    end
close cur
deallocate cur";

    public static SqlPreCommand RemoveAllScript(DatabaseName? databaseName)
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        var systemSchemas = sqlBuilder.SystemSchemas.ToString(a => "'" + a + "'", ", ");
        var systemSchemasExeptDbo = sqlBuilder.SystemSchemas.Where(s => s != "dbo").ToString(a => "'" + a + "'", ", ");

        return SqlPreCommand.Combine(Spacing.Double,
            new SqlPreCommandSimple(Use(databaseName, RemoveAllProceduresScript)),
            new SqlPreCommandSimple(Use(databaseName, RemoveAllViewsScript).FormatWith(systemSchemasExeptDbo)),
            new SqlPreCommandSimple(Use(databaseName, RemoveAllConstraintsScript)),
            Connector.Current.SupportsTemporalTables ? new SqlPreCommandSimple(Use(databaseName, StopSystemVersioning)) : null,
            new SqlPreCommandSimple(Use(databaseName, RemoveAllTablesScript)),
            new SqlPreCommandSimple(Use(databaseName, RemoveAllSchemasScript.FormatWith(systemSchemas)))
            )!;
    }

    static string Use(DatabaseName? databaseName, string script)
    {
        if (databaseName == null)
            return script;

        return "use " + databaseName + "\r\n" + script;
    }
}
