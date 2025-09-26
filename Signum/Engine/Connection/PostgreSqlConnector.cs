using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;
using Npgsql;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using Signum.Engine.Sync.Postgres;
using Signum.Utilities.Reflection;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Signum.Engine;


public static class PostgresVersionDetector
{
    public static Version? Detect(string connectionString, Version? fallback)
    {
        try 
        {
            using (NpgsqlConnection con = new NpgsqlConnection(connectionString))
            {
                var sql = @"SHOW server_version;";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, con))
                {
                    NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd);

                    DataTable result = new DataTable();
                    da.Fill(result);

                    var version = (string)result.Rows[0]["server_version"]!;

                    return new Version(version.TryBefore("(") ?? version);
                }
            }
        }
        catch
        {
            return fallback;
        }
    }
}

public class PostgreSqlConnector : Connector
{
    public ResetLazy<string> LocalTimeZoneLazy = new ResetLazy<string>(() => (string)Executor.ExecuteScalar("SELECT current_setting('TIMEZONE')")!);
    public override string LocalTimeZone => LocalTimeZoneLazy.Value;

    public override ParameterBuilder ParameterBuilder { get; protected set; }

    public Version? PostgresVersion { get; set; }

    Action<NpgsqlSlimDataSourceBuilder>? customizeBuilder;

    string originalDatabaseName;
    public PostgreSqlConnector(string connectionString, Schema schema, Version? postgresVersion, Action<NpgsqlSlimDataSourceBuilder>? customizeBuilder = null) : base(schema.Do(s => s.Settings.IsPostgres = true))
    {
        this.originalDatabaseName = new NpgsqlConnectionStringBuilder(connectionString).Database!;
        this.customizeBuilder = customizeBuilder;
        this.ChangeConnectionString(connectionString, runCustomizer: true);
        this.ConnectionString = connectionString;
        this.ParameterBuilder = new PostgreSqlParameterBuilder();
        this.PostgresVersion = postgresVersion;
    }

    public void ChangeConnectionStringDatabase(string databaseName, bool runCustomizer = true)
    {
        NpgsqlConnectionStringBuilder csb = new NpgsqlConnectionStringBuilder(this.ConnectionString);
        csb.Database = databaseName;
        ChangeConnectionString(csb.ToString(), runCustomizer);
    }

    public void ChangeConnectionString(string connectionString, bool runCustomizer = true)
    {
        this.ConnectionString = connectionString;
        var builder = new NpgsqlSlimDataSourceBuilder(connectionString);
        if (runCustomizer)
            customizeBuilder?.Invoke(builder);

        this.DataSource = builder.Build();
    }

    public override int MaxNameLength => 63;

    public int? CommandTimeout { get; set; } = null;
    public NpgsqlDataSource DataSource { get; private set; }
    public string ConnectionString { get; private set; }

    public override bool AllowsMultipleQueries => true;

    public override bool SupportsScalarSubquery => true;

    public override bool SupportsScalarSubqueryInAggregates => true;

    public override bool AllowsSetSnapshotIsolation => false;

    public override bool AllowsConvertToDate => true;

    public override bool SupportsStringAggr => true;

    public override bool AllowsConvertToTime => true;

    public override bool SupportsSqlDependency => false;

    public override bool SupportsFormat => true;

    public override bool SupportsTemporalTables => true;

    public override bool RequiresRetry => false;

    public override bool SupportsDateDifBig => false;

    public override bool SupportsPartitioning => false; //for now

    public override bool AllowsIndexWithWhere(string where) => true;

    public override Connector ForDatabase(Maps.DatabaseName? database)
    {
        if (database == null)
            return this;

        throw new NotImplementedException("ForDatabase " + database);
    }

    public override void CleanDatabase(DatabaseName? database)
    {
        PostgreSqlConnectorScripts.RemoveAllScript(database).ExecuteNonQuery();
    }

    public override DbParameter CloneParameter(DbParameter p)
    {
        NpgsqlParameter sp = (NpgsqlParameter)p;
        return new NpgsqlParameter(sp.ParameterName, sp.Value) { IsNullable = sp.IsNullable, NpgsqlDbType = sp.NpgsqlDbType };
    }

    public override DbConnection CreateConnection()
    {
        return DataSource.CreateConnection();
    }

    public override string OriginalDatabaseName()
    {
        return this.originalDatabaseName;
    }

    public override string DatabaseName()
    {
        return new NpgsqlConnectionStringBuilder(ConnectionString).Database!;
    }

    public override string DataSourceName()
    {
        return new NpgsqlConnection(ConnectionString).DataSource;
    }

    public override string GetSqlDbType(DbParameter p)
    {
        return ((NpgsqlParameter)p).NpgsqlDbType.ToString().ToUpperInvariant();
    }

    public override void RollbackTransactionPoint(DbTransaction transaction, string savePointName)
    {
        ((NpgsqlTransaction)transaction).Rollback(savePointName);
    }

    public override void SaveTransactionPoint(DbTransaction transaction, string savePointName)
    {
        ((NpgsqlTransaction)transaction).Save(savePointName);
    }


    T EnsureConnectionRetry<T>(Func<NpgsqlConnection?, T> action)
    {
        if (Transaction.HasTransaction)
            return action(null);

        using (NpgsqlConnection con = this.DataSource.OpenConnection())
        {
            return action(con);
        }
    }

    NpgsqlCommand NewCommand(SqlPreCommandSimple preCommand, NpgsqlConnection? overridenConnection, CommandType commandType)
    {
        NpgsqlCommand cmd = new NpgsqlCommand { CommandType = commandType };

        int? timeout = Connector.ScopeTimeout ?? CommandTimeout;
        if (timeout.HasValue)
            cmd.CommandTimeout = timeout.Value;

        if (overridenConnection != null)
            cmd.Connection = overridenConnection;
        else
        {
            cmd.Connection = (NpgsqlConnection)Transaction.CurrentConnection!;
            cmd.Transaction = (NpgsqlTransaction)Transaction.CurrentTransaction!;
        }

        cmd.CommandText = preCommand.Sql;

        if (preCommand.Parameters != null)
        {
            foreach (NpgsqlParameter param in preCommand.Parameters)
            {
                cmd.Parameters.Add(param);
            }
        }

        Log(preCommand);

        return cmd;
    }

    protected internal override void BulkCopy(DataTable dt, List<IColumn> columns, ObjectName destinationTable, SqlBulkCopyOptions options, int? timeout)
    {
        EnsureConnectionRetry(con =>
        {
            try
            {
                con = con ?? (NpgsqlConnection)Transaction.CurrentConnection!;

                bool isPostgres = true;

                var columnsSql = dt.Columns.Cast<DataColumn>().ToString(a => a.ColumnName.SqlEscape(isPostgres), ", ");
                using (var writer = con.BeginBinaryImport($"COPY {destinationTable} ({columnsSql}) FROM STDIN (FORMAT BINARY)"))
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var row = dt.Rows[i];
                        writer.StartRow();
                        for (int j = 0; j < dt.Columns.Count; j++)
                        {
                            var col = dt.Columns[j];
                            writer.Write(row[col], columns[j].DbType.PostgreSql);
                        }
                    }

                    writer.Complete();
                    return 0;
                }
            }
            catch (Exception ex)
            {
                var nex = HandleException(ex, new SqlPreCommandSimple($"## BeginBinaryImport into {destinationTable} ({columns.ToString(a => "'" + a.Name + "'", ", ")})"));
                if (nex == ex)
                    throw;

                throw nex;
            }

        });
    }

    protected internal override DataTable ExecuteDataTable(SqlPreCommandSimple preCommand, CommandType commandType)
    {
        return EnsureConnectionRetry(con =>
        {
            using (NpgsqlCommand cmd = NewCommand(preCommand, con, commandType))
            using (HeavyProfiler.Log("SQL", () => preCommand.sp_executesql()))
            {
                try
                {
                    NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd);

                    DataTable result = new DataTable();
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

    protected internal override int ExecuteNonQuery(SqlPreCommandSimple preCommand, CommandType commandType)
    {
        return EnsureConnectionRetry(con =>
        {
            using (NpgsqlCommand cmd = NewCommand(preCommand, con, commandType))
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

    protected internal override object? ExecuteScalar(SqlPreCommandSimple preCommand, CommandType commandType)
    {
        return EnsureConnectionRetry(con =>
        {
            using (NpgsqlCommand cmd = NewCommand(preCommand, con, commandType))
            using (HeavyProfiler.Log("SQL", () => preCommand.sp_executesql()))
            {
                try
                {
                    object? result = cmd.ExecuteScalar();

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

    public Exception HandleException(Exception ex, SqlPreCommandSimple command)
    {
        var nex = ReplaceException(ex, command);
        nex.Data["Sql"] = command.sp_executesql();
        nex.Data["SqlRAW"] = command.Sql;
        return nex;
    }

    Exception ReplaceException(Exception ex, SqlPreCommandSimple command)
    {
        if (ex is Npgsql.PostgresException se)
        {
            switch (se.SqlState)
            {
                //case "-2": return new TimeoutException(ex.Message, ex);
                case "23505": return new UniqueKeyException(ex);
                case "23503": return new ForeignKeyException(ex);
                default: return ex;
            }
        }

        //if (ex is SqlTypeException ste && ex.Message.Contains("DateTime"))
        //{
        //    var mins = command.Parameters.Where(a => DateTime.MinValue.Equals(a.Value));

        //    if (mins.Any())
        //    {
        //        return new ArgumentOutOfRangeException("{0} {1} not initialized and equal to DateTime.MinValue".FormatWith(
        //            mins.CommaAnd(a => a.ParameterName),
        //            mins.Count() == 1 ? "is" : "are"), ex);
        //    }
        //}

        return ex;
    }


    public override string ToString() => $"PostgreSqlConnector({PostgresVersion}, Database: {this.DatabaseName()}, DataSource: {this.DataSourceName()})";

    public override bool HasTables()
    {
        return (from ns in Database.View<PgNamespace>()
                where !ns.IsInternal()
                from t in ns.Tables()
                select t).Any();
    }

    public void ReloadTypes()
    {
        using (var conn = this.CreateConnection())
        {
            conn.Open();
            ((NpgsqlConnection)conn).ReloadTypes();
        }
    }
}

public static class PostgreSqlConnectorScripts
{
    public static SqlPreCommandSimple RemoveAllScript(DatabaseName? databaseName)
    {
        if (databaseName != null)
            throw new NotSupportedException();

        return new SqlPreCommandSimple(@"-- Copyright © 2019
--      mirabilos <t.glaser@tarent.de>
--
-- Provided that these terms and disclaimer and all copyright notices
-- are retained or reproduced in an accompanying document, permission
-- is granted to deal in this work without restriction, including un‐
-- limited rights to use, publicly perform, distribute, sell, modify,
-- merge, give away, or sublicence.
--
-- This work is provided “AS IS” and WITHOUT WARRANTY of any kind, to
-- the utmost extent permitted by applicable law, neither express nor
-- implied; without malicious intent or gross negligence. In no event
-- may a licensor, author or contributor be held liable for indirect,
-- direct, other damage, loss, or other issues arising in any way out
-- of dealing in the work, even if advised of the possibility of such
-- damage or existence of a defect, except proven that it results out
-- of said person’s immediate fault when using the work as intended.
-- -
-- Drop everything from the PostgreSQL database.

DO $$
DECLARE
        r RECORD;
BEGIN
        -- triggers
        FOR r IN (SELECT pns.nspname, pc.relname, pt.tgname
                FROM pg_trigger pt, pg_class pc, pg_namespace pns
                WHERE pns.oid=pc.relnamespace AND pc.oid=pt.tgrelid
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
                    AND pt.tgisinternal=false
            ) LOOP
                EXECUTE format('DROP TRIGGER %I ON %I.%I;',
                    r.tgname, r.nspname, r.relname);
        END LOOP;
        -- constraints #1: foreign key
        FOR r IN (SELECT pns.nspname, pc.relname, pcon.conname
                FROM pg_constraint pcon, pg_class pc, pg_namespace pns
                WHERE pns.oid=pc.relnamespace AND pc.oid=pcon.conrelid
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
                    AND pcon.contype='f'
            ) LOOP
                EXECUTE format('ALTER TABLE ONLY %I.%I DROP CONSTRAINT %I;',
                    r.nspname, r.relname, r.conname);
        END LOOP;
        -- constraints #2: the rest
        FOR r IN (SELECT pns.nspname, pc.relname, pcon.conname
                FROM pg_constraint pcon, pg_class pc, pg_namespace pns
                WHERE pns.oid=pc.relnamespace AND pc.oid=pcon.conrelid
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
                    AND pcon.contype<>'f'
            ) LOOP
                EXECUTE format('ALTER TABLE ONLY %I.%I DROP CONSTRAINT %I;',
                    r.nspname, r.relname, r.conname);
        END LOOP;
        -- indicēs
        FOR r IN (SELECT pns.nspname, pc.relname
                FROM pg_class pc, pg_namespace pns
                WHERE pns.oid=pc.relnamespace
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
                    AND pc.relkind='i'
            ) LOOP
                EXECUTE format('DROP INDEX %I.%I;',
                    r.nspname, r.relname);
        END LOOP;
        -- normal and materialised views
        FOR r IN (SELECT pns.nspname, pc.relname
                FROM pg_class pc, pg_namespace pns
                WHERE pns.oid=pc.relnamespace
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
                    AND pc.relname NOT LIKE 'pg_%'
                    AND pc.relkind IN ('v', 'm')
            ) LOOP
                EXECUTE format('DROP VIEW %I.%I;',
                    r.nspname, r.relname);
        END LOOP;
        -- tables
        FOR r IN (SELECT pns.nspname, pc.relname
                FROM pg_class pc, pg_namespace pns
                WHERE pns.oid=pc.relnamespace
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
                    AND pc.relkind='r'
            ) LOOP
                EXECUTE format('DROP TABLE %I.%I;',
                    r.nspname, r.relname);
        END LOOP;
        -- sequences
        FOR r IN (SELECT pns.nspname, pc.relname
                FROM pg_class pc, pg_namespace pns
                WHERE pns.oid=pc.relnamespace
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
                    AND pc.relkind='S'
            ) LOOP
                EXECUTE format('DROP SEQUENCE %I.%I;',
                    r.nspname, r.relname);
        END LOOP;
        -- extensions (see below), only if necessary
        FOR r IN (SELECT pns.nspname, pe.extname
                FROM pg_extension pe, pg_namespace pns
                WHERE pns.oid=pe.extnamespace
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
            ) LOOP
                EXECUTE format('DROP EXTENSION %I;', r.extname);
        END LOOP;
        -- functions / procedures
        FOR r IN (SELECT pns.nspname, pp.proname, pp.oid
                FROM pg_proc pp, pg_namespace pns
                WHERE pns.oid=pp.pronamespace
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
            ) LOOP
                EXECUTE format('DROP FUNCTION %I.%I(%s);',
                    r.nspname, r.proname,
                    pg_get_function_identity_arguments(r.oid));
        END LOOP;
        -- nōn-default schemata we own; assume to be run by a not-superuser
        FOR r IN (SELECT pns.nspname
                FROM pg_namespace pns, pg_roles pr
                WHERE pr.oid=pns.nspowner
                    AND pns.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast', 'public')
                    AND pr.rolname=current_user
            ) LOOP
                EXECUTE format('DROP SCHEMA %I;', r.nspname);
        END LOOP;
        -- voilà
        RAISE NOTICE 'Database cleared!';
END; $$;");
    }
}

public class PostgreSqlParameterBuilder : ParameterBuilder
{
    public override DbParameter CreateParameter(string parameterName, AbstractDbType dbType, string? udtTypeName, bool nullable, DateTimeKind datetimeKind, object? value)
    {
        if (dbType.IsDate())
        {
            if (value is DateTime dt)
                AssertDateTime(dt, datetimeKind);
        }

        if(dbType.PostgreSql == NpgsqlTypes.NpgsqlDbType.LTree && value is SqlHierarchyId shi)
        {
            value = shi.ToSortableString();
        }

        var result = new Npgsql.NpgsqlParameter(parameterName, value ?? DBNull.Value)
        {
            IsNullable = nullable
        };

        result.NpgsqlDbType = dbType.PostgreSql;
        if (udtTypeName != null)
            result.DataTypeName = udtTypeName;


        return result;
    }

    protected static MethodInfo miToSortableString = ReflectionTools.GetMethodInfo((SqlHierarchyId? s) => s.ToSortableString());


    public override MemberInitExpression ParameterFactory(Expression parameterName, AbstractDbType dbType, int? size, byte? precision, byte? scale, string? udtTypeName, bool nullable, DateTimeKind dateTimeKind, Expression value)
    {
        var uType = value.Type.UnNullify();
        var exp =
             uType == typeof(DateTime) ? Expression.Call(miAsserDateTime, Expression.Convert(value, typeof(DateTime?)), Expression.Constant(dateTimeKind)) :
             uType == typeof(SqlHierarchyId) ? Expression.Coalesce(
                    Expression.Call(miToSortableString, Expression.Convert(value, typeof(SqlHierarchyId?))), 
                    Expression.Constant(DBNull.Value, typeof(object))) :
             ////https://github.com/dotnet/SqlClient/issues/1009
             //uType == typeof(DateOnly) ? Expression.Call(miToDateTimeKind, Expression.Convert(value, typeof(DateOnly)), Expression.Constant(Schema.Current.DateTimeKind)) :
             //uType == typeof(TimeOnly) ? Expression.Call(Expression.Convert(value, typeof(TimeOnly)), miToTimeSpan) :
             value;


        Expression valueExpr = Expression.Convert(exp, typeof(object));

        if (nullable)
        {
            valueExpr = Expression.Condition(Expression.Equal(value, Expression.Constant(null, value.Type)),
                Expression.Constant(DBNull.Value, typeof(object)),
                valueExpr);
        }

        NewExpression newExpr = Expression.New(typeof(NpgsqlParameter).GetConstructor(new[] { typeof(string), typeof(object) })!, parameterName, valueExpr);


        List<MemberBinding> mb = new List<MemberBinding>()
            {
                Expression.Bind(typeof(NpgsqlParameter).GetProperty(nameof(NpgsqlParameter.IsNullable))!, Expression.Constant(nullable)),
                Expression.Bind(typeof(NpgsqlParameter).GetProperty(nameof(NpgsqlParameter.NpgsqlDbType))!, Expression.Constant(dbType.PostgreSql)),
            };

        if (size != null)
            mb.Add(Expression.Bind(typeof(NpgsqlParameter).GetProperty(nameof(NpgsqlParameter.Size))!, Expression.Constant(size)));

        if (precision != null)
            mb.Add(Expression.Bind(typeof(NpgsqlParameter).GetProperty(nameof(NpgsqlParameter.Precision))!, Expression.Constant(precision)));

        if (scale != null)
            mb.Add(Expression.Bind(typeof(NpgsqlParameter).GetProperty(nameof(NpgsqlParameter.Scale))!, Expression.Constant(scale)));

        if (udtTypeName != null)
            mb.Add(Expression.Bind(typeof(NpgsqlParameter).GetProperty(nameof(NpgsqlParameter.DataTypeName))!, Expression.Constant(udtTypeName)));

        return Expression.MemberInit(newExpr, mb);
    }
}

public class PostgresStatementSplitter
{
    public static List<string> SplitPostgresScript(string sql)
    {
        var statements = new List<string>();
        var sb = new StringBuilder();
        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        string? inDollarTag = null;
        int i = 0;

        while (i < sql.Length)
        {
            char c = sql[i];
            sb.Append(c);

            if (inDollarTag != null)
            {
                if (sql[i] == '$' && sql.Substring(i).StartsWith(inDollarTag))
                {
                    sb.Append(inDollarTag.Substring(1)); // we've already added one '$'
                    i += inDollarTag.Length - 1;
                    inDollarTag = null;
                }
            }
            else if (inSingleQuote)
            {
                if (c == '\'' && (i + 1 >= sql.Length || sql[i + 1] != '\''))
                {
                    inSingleQuote = false;
                }
                else if (c == '\'' && sql[i + 1] == '\'')
                {
                    sb.Append('\''); // escape quote
                    i++;
                }
            }
            else if (inDoubleQuote)
            {
                if (c == '"') inDoubleQuote = false;
            }
            else
            {
                if (c == '\'') inSingleQuote = true;
                else if (c == '"') inDoubleQuote = true;
                else if (c == '$')
                {
                    // Look ahead to detect a dollar-quote tag like $$, $tag$
                    var match = Regex.Match(sql.Substring(i), @"^\$[A-Za-z0-9_]*\$");
                    if (match.Success)
                    {
                        inDollarTag = match.Value;
                        sb.Append(match.Value.Substring(1)); // We've already added one '$'
                        i += match.Length - 1;
                    }
                }
                else if (c == ';')
                {
                    var statement = sb.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(statement))
                        statements.Add(statement);
                    sb.Clear();
                }
            }

            i++;
        }

        var final = sb.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(final))
            statements.Add(final);

        return statements;
    }
}
