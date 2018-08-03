using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Signum.Utilities;
using Signum.Engine.Maps;
using System.Linq;
using System.IO;
using System.Data.SqlClient;
using Signum.Utilities.ExpressionTrees;
using System.Text.RegularExpressions;
using Signum.Engine.DynamicQuery;
using System.Data.Common;
using System.Linq.Expressions;
using Signum.Entities;
using Signum.Utilities.Reflection;
using System.Reflection;
using Microsoft.SqlServer.Server;
using System.Threading.Tasks;
using System.Threading;

namespace Signum.Engine
{

    public abstract class Connector
    {
        static readonly Variable<Connector> currentConnection = Statics.ThreadVariable<Connector>("connection");

        public static IDisposable Override(Connector connection)
        {
            Connector oldConnection = currentConnection.Value;

            currentConnection.Value = connection;

            return new Disposable(() => currentConnection.Value = oldConnection);
        }

        public static Connector Current
        {
            get { return currentConnection.Value ?? Default; }
        }

        static Connector @default;
        public static Connector Default
        {
            get { return @default; }
            set { @default = value; }
        }

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
        }

        public Schema Schema { get; private set; }

        static readonly Variable<TextWriter> logger = Statics.ThreadVariable<TextWriter>("connectionlogger");
        public static TextWriter CurrentLogger
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
                            p.Value?.Let(v => CSharpRenderer.Value(v, v.GetType(), null))), "\r\n"));
                log.WriteLine();
            }
        }

        public abstract SqlDbType GetSqlDbType(DbParameter p);

        protected internal abstract object ExecuteScalar(SqlPreCommandSimple preCommand, CommandType commandType);
        protected internal abstract int ExecuteNonQuery(SqlPreCommandSimple preCommand, CommandType commandType);
        protected internal abstract DataTable ExecuteDataTable(SqlPreCommandSimple command, CommandType commandType);
        protected internal abstract DbDataReader UnsafeExecuteDataReader(SqlPreCommandSimple sqlPreCommandSimple, CommandType commandType);
        protected internal abstract Task<DbDataReader> UnsafeExecuteDataReaderAsync(SqlPreCommandSimple preCommand, CommandType commandType, CancellationToken token);
        protected internal abstract DataSet ExecuteDataSet(SqlPreCommandSimple sqlPreCommandSimple, CommandType commandType);
        protected internal abstract void BulkCopy(DataTable dt, ObjectName destinationTable, SqlBulkCopyOptions options, int? timeout);

        public abstract string DatabaseName();

        public abstract string DataSourceName();

        public virtual int MaxNameLength { get { return 128; } }

        public abstract void SaveTransactionPoint(DbTransaction transaction, string savePointName);

        public abstract void RollbackTransactionPoint(DbTransaction Transaction, string savePointName);

        public abstract DbParameter CloneParameter(DbParameter p);

        public abstract DbConnection CreateConnection();

        public IsolationLevel IsolationLevel { get; set; }

        public abstract ParameterBuilder ParameterBuilder { get; protected set; }

        public abstract void CleanDatabase(DatabaseName database);

        public abstract bool AllowsMultipleQueries { get; }

        public abstract bool SupportsScalarSubquery { get; }
        public abstract bool SupportsScalarSubqueryInAggregates { get; }


        public static string TryExtractDatabaseNameWithPostfix(ref string connectionString, string catalogPostfix)
        {
            string toFind = "+" + catalogPostfix;

            string result = connectionString.TryBefore(toFind).TryAfterLast("=");
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

        public abstract bool AllowsSetSnapshotIsolation { get; }

        public abstract bool AllowsIndexWithWhere(string where);

        public abstract SqlPreCommand ShrinkDatabase(string schemaName);

        public abstract bool AllowsConvertToDate { get; }

        public abstract bool AllowsConvertToTime { get; }

        public abstract bool SupportsSqlDependency { get; }

        public abstract bool SupportsFormat { get; }

        public abstract bool SupportsTemporalTables { get; }
    }

    public abstract class ParameterBuilder
    {
        public static string GetParameterName(string name)
        {
            return "@" + name;
        }

        public DbParameter CreateReferenceParameter(string parameterName, PrimaryKey? id, IColumn column)
        {
            return CreateParameter(parameterName, column.SqlDbType, null, column.Nullable.ToBool(), id == null ? (object)null : id.Value.Object);
        }

        public DbParameter CreateParameter(string parameterName, object value, Type type)
        {
            var pair = Schema.Current.Settings.GetSqlDbTypePair(type.UnNullify());

            return CreateParameter(parameterName, pair.SqlDbType, pair.UserDefinedTypeName, type == null || type.IsByRef || type.IsNullable(), value);
        }

        public abstract DbParameter CreateParameter(string parameterName, SqlDbType type, string udtTypeName, bool nullable, object value);
        public abstract MemberInitExpression ParameterFactory(Expression parameterName, SqlDbType type, string udtTypeName, bool nullable, Expression value);

        protected static bool IsDate(SqlDbType type)
        {
            return type == SqlDbType.Date || type == SqlDbType.DateTime || type == SqlDbType.DateTime2 || type == SqlDbType.SmallDateTime;
        }



        protected static MethodInfo miAsserDateTime = ReflectionTools.GetMethodInfo(() => AssertDateTime(null));
        protected static DateTime? AssertDateTime(DateTime? dateTime)
        {
            if (Schema.Current.TimeZoneMode == TimeZoneMode.Utc && dateTime.HasValue && dateTime.Value.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("Attempt to use a non-Utc date in the database");

            return dateTime;
        }
    }
}
