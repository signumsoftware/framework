using System.Data;
using Signum.Engine.Maps;
using System.IO;
using System.Data.Common;
using Signum.Utilities.Reflection;
using Microsoft.Data.SqlClient;

namespace Signum.Engine;


public abstract class Connector
{
    static readonly Variable<Connector> currentConnector = Statics.ThreadVariable<Connector>("connection");

    public SqlBuilder SqlBuilder;

    public static IDisposable Override(Connector connector)
    {
        Connector oldConnection = currentConnector.Value;

        currentConnector.Value = connector;

        return new Disposable(() => currentConnector.Value = oldConnection);
    }

    public static Connector Current
    {
        get { return currentConnector.Value ?? Default; }
    }

    public static Connector Default { get; set; } = null!;

    static readonly Variable<int?> scopeTimeout = Statics.ThreadVariable<int?>("scopeTimeout");
    public static int? ScopeTimeout { get { return scopeTimeout.Value; } }
    public static IDisposable CommandTimeoutScope(int? timeoutSeconds)
    {
        var old = scopeTimeout.Value;
        scopeTimeout.Value = timeoutSeconds;
        return new Disposable(() => scopeTimeout.Value = old);
    }

    public Connector(Schema schema)
    {
        this.Schema = schema;
        this.IsolationLevel = IsolationLevel.Unspecified;
        this.SqlBuilder = new SqlBuilder(this);
    }

    public Schema Schema { get; private set; }

    static readonly Variable<TextWriter?> logger = Statics.ThreadVariable<TextWriter?>("connectionlogger");
    public static TextWriter? CurrentLogger
    {
        get { return logger.Value; }
        set { logger.Value = value; }
    }

    protected static void Log(SqlPreCommandSimple pcs)
    {
        var log = logger.Value;
        if (log != null)
        {
            log.WriteLine(pcs.Sql);
            if (pcs.Parameters != null)
                log.WriteLine(pcs.Parameters
                    .ToString(p => "{0} {1}: {2}".FormatWith(
                        p.ParameterName,
                        Connector.Current.GetSqlDbType(p),
                        p.Value?.Let(v => v.ToString())), "\r\n"));
            log.WriteLine();
        }
    }

    public abstract string GetSqlDbType(DbParameter p);

    protected internal abstract object? ExecuteScalar(SqlPreCommandSimple preCommand, CommandType commandType);
    protected internal abstract int ExecuteNonQuery(SqlPreCommandSimple preCommand, CommandType commandType);
    protected internal abstract DataTable ExecuteDataTable(SqlPreCommandSimple preCommand, CommandType commandType);
    protected internal abstract DbDataReaderWithCommand UnsafeExecuteDataReader(SqlPreCommandSimple preCommand, CommandType commandType);
    protected internal abstract Task<DbDataReaderWithCommand> UnsafeExecuteDataReaderAsync(SqlPreCommandSimple preCommand, CommandType commandType, CancellationToken token);
    protected internal abstract void BulkCopy(DataTable dt, List<IColumn> columns, ObjectName destinationTable, SqlBulkCopyOptions options, int? timeout);

    public abstract Connector ForDatabase(Maps.DatabaseName? database);

    public abstract string DatabaseName();

    public abstract string DataSourceName();

    public abstract int MaxNameLength { get; }

    public abstract void SaveTransactionPoint(DbTransaction transaction, string savePointName);

    public abstract void RollbackTransactionPoint(DbTransaction Transaction, string savePointName);

    public abstract DbParameter CloneParameter(DbParameter p);

    public abstract DbConnection CreateConnection();

    public IsolationLevel IsolationLevel { get; set; }

    public abstract ParameterBuilder ParameterBuilder { get; protected set; }

    public abstract void CleanDatabase(DatabaseName? database);

    public abstract bool AllowsMultipleQueries { get; }

    public abstract bool SupportsScalarSubquery { get; }
    public abstract bool SupportsScalarSubqueryInAggregates { get; }


    public static string? TryExtractDatabaseNameWithPostfix(ref string connectionString, string catalogPostfix)
    {
        string toFind = "+" + catalogPostfix;

        string? result = connectionString.TryBefore(toFind).TryAfterLast("=");
        if (result == null)
            return null;

        connectionString = connectionString.Replace(toFind, ""); // Remove toFind

        return result + catalogPostfix;
    }

    public static string ExtractCatalogPostfix(ref string connectionString, string catalogPostfix)
    {
        string toFind = "+" + catalogPostfix;

        int index = connectionString.IndexOf(toFind);
        if (index == -1)
            throw new InvalidOperationException("CatalogPostfix '{0}' not found in the connection string".FormatWith(toFind));

        connectionString = connectionString.Substring(0, index) + connectionString.Substring(index + toFind.Length); // Remove toFind

        return catalogPostfix;
    }

    public abstract bool HasTables();

    public abstract bool AllowsSetSnapshotIsolation { get; }

    public abstract bool AllowsIndexWithWhere(string where);

    public abstract bool AllowsConvertToDate { get; }

    public abstract bool AllowsConvertToTime { get; }

    public abstract bool SupportsSqlDependency { get; }

    public abstract bool SupportsFormat { get; }

    public abstract bool SupportsTemporalTables { get; }

    public abstract bool RequiresRetry { get; }

    public abstract bool SupportsDateDifBig { get; }
}

public abstract class ParameterBuilder
{
    public static string GetParameterName(string name)
    {
        return "@" + name;
    }

    public DbParameter CreateReferenceParameter(string parameterName, PrimaryKey? id, IColumn column)
    {
        return CreateParameter(parameterName, column.DbType, null, column.Nullable.ToBool(), id == null ? null : id.Value.Object);
    }

    public DbParameter CreateParameter(string parameterName, object? value, Type type)
    {
        var pair = Schema.Current.Settings.GetSqlDbTypePair(type.UnNullify());

        return CreateParameter(parameterName, pair.DbType, pair.UserDefinedTypeName, type == null || type.IsByRef || type.IsNullable(), value);
    }

    public abstract DbParameter CreateParameter(string parameterName, AbstractDbType dbType, string? udtTypeName, bool nullable, object? value);
    public abstract MemberInitExpression ParameterFactory(Expression parameterName, AbstractDbType dbType, int? size, byte? precision, byte? scale, string? udtTypeName, bool nullable, Expression value);

    protected static MethodInfo miAsserDateTime = ReflectionTools.GetMethodInfo(() => AssertDateTime(null));
    protected static MethodInfo miToDateTimeKind = ReflectionTools.GetMethodInfo(() => DateOnly.MinValue.ToDateTime(DateTimeKind.Utc));
    protected static MethodInfo miToTimeSpan = ReflectionTools.GetMethodInfo(() => TimeOnly.MaxValue.ToTimeSpan());
    protected static DateTime? AssertDateTime(DateTime? dateTime)
    {

        if (dateTime.HasValue)
        {
            if (Schema.Current.TimeZoneMode == TimeZoneMode.Utc && dateTime.Value.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("Attempt to use a non-Utc date in the database");

            //Problematic with Time machine
            //if (Schema.Current.TimeZoneMode != TimeZoneMode.Utc && dateTime.Value.Kind == DateTimeKind.Utc)
            //    throw new InvalidOperationException("Attempt to use a Utc date in the database");
        }

        return dateTime;
    }
}

public class DbDataReaderWithCommand : IDisposable
{
    public DbDataReaderWithCommand(DbCommand command, DbDataReader reader)
    {
        Command = command;
        Reader = reader;
    }

    public DbCommand Command { get; private set; }
    public DbDataReader Reader { get; private set; }

    public void Dispose()
    {
        Reader.Dispose();
    }
}
