﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Exceptions;
using System.Data.SqlTypes;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Microsoft.SqlServer.Types;
using Microsoft.SqlServer.Server;

namespace Signum.Engine
{
    public enum SqlServerVersion
    {
        SqlServer2005,
        SqlServer2008,
        SqlServer2012,
        SqlServer2014,
        AzureSQL,
    }

    public class SqlConnector : Connector
    {
        int? commandTimeout = null;
        string connectionString;

        public SqlServerVersion Version { get; set; }

        public SqlConnector(string connectionString, Schema schema, DynamicQueryManager dqm, SqlServerVersion version)
            : base(schema, dqm)
        {
            this.connectionString = connectionString;
            this.ParameterBuilder = new SqlParameterBuilder();

            this.Version = version;
            // In disconnected scenarios schema can be null, this must be checked.
            if (version >= SqlServerVersion.SqlServer2008 && schema != null)
            {
                var s = schema.Settings;

                if (!s.TypeValues.ContainsKey(typeof(TimeSpan)))
                    schema.Settings.TypeValues.Add(typeof(TimeSpan), SqlDbType.Time);

                if (!s.UdtSqlName.ContainsKey(typeof(SqlHierarchyId)))
                    s.UdtSqlName.Add(typeof(SqlHierarchyId), "HierarchyId");

                if (!s.UdtSqlName.ContainsKey(typeof(SqlGeography)))
                    s.UdtSqlName.Add(typeof(SqlGeography), "Geography");

                if (!s.UdtSqlName.ContainsKey(typeof(SqlGeometry)))
                    s.UdtSqlName.Add(typeof(SqlGeometry), "Geometry");
            }
        }

        public int? CommandTimeout
        {
            get { return commandTimeout; }
            set { commandTimeout = value; }
        }

        public string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value; }
        }

        public override bool SupportsScalarSubquery { get { return true; } }
        public override bool SupportsScalarSubqueryInAggregates { get { return false; } }

        SqlConnection EnsureConnection()
        {
            if (Transaction.HasTransaction)
                return null;

            SqlConnection result = new SqlConnection(this.ConnectionString);
            result.Open();
            return result;
        }

        SqlCommand NewCommand(SqlPreCommandSimple preCommand, SqlConnection overridenConnection)
        {
            SqlCommand cmd = new SqlCommand();

            int? timeout = Connector.ScopeTimeout ?? CommandTimeout;
            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            if (overridenConnection != null)
                cmd.Connection = overridenConnection;
            else
            {
                cmd.Connection = (SqlConnection)Transaction.CurrentConnection;
                cmd.Transaction = (SqlTransaction)Transaction.CurrentTransaccion;
            }

            cmd.CommandText = preCommand.Sql;

            if (preCommand.Parameters != null)
            {
                foreach (SqlParameter param in preCommand.Parameters)
                {
                    cmd.Parameters.Add(param);
                }
            }

            Log(preCommand);

            return cmd;
        }

        protected internal override object ExecuteScalar(SqlPreCommandSimple preCommand)
        {
            using (SqlConnection con = EnsureConnection())
            using (SqlCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", () => preCommand.PlainSql()))
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
        }

        protected internal override int ExecuteNonQuery(SqlPreCommandSimple preCommand)
        {
            using (SqlConnection con = EnsureConnection())
            using (SqlCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", () => preCommand.PlainSql()))
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
        }

        public void ExecuteDataReaderDependency(SqlPreCommandSimple preCommand, OnChangeEventHandler change, Action reconect, Action<FieldReader> forEach)
        {
            bool reconected = false; 
            retry:
            try
            {
                using (SqlConnection con = EnsureConnection())
                using (SqlCommand cmd = NewCommand(preCommand, con))
                using (HeavyProfiler.Log("SQL-Dependency"))
                using (HeavyProfiler.Log("SQL", () => preCommand.PlainSql()))
                {
                    try
                    {
                        if (change != null)
                        {
                            SqlDependency dep = new SqlDependency(cmd);
                            dep.OnChange += change;
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            FieldReader fr = new FieldReader(reader);
                            int row = -1;
                            try
                            {
                                while (reader.Read())
                                {
                                    row++;
                                    forEach(fr);
                                }
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

        protected internal override DbDataReader UnsafeExecuteDataReader(SqlPreCommandSimple preCommand)
        {
            try
            {
                SqlCommand cmd = NewCommand(preCommand, null);

                return cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                var nex = HandleException(ex, preCommand);
                if (nex == ex)
                    throw;

                throw nex;
            }
        }

        protected internal override DataTable ExecuteDataTable(SqlPreCommandSimple preCommand)
        {
            using (SqlConnection con = EnsureConnection())
            using (SqlCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", () => preCommand.PlainSql()))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(cmd);

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
        }

        protected internal override DataSet ExecuteDataSet(SqlPreCommandSimple preCommand)
        {
            using (SqlConnection con = EnsureConnection())
            using (SqlCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", () => preCommand.PlainSql()))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet result = new DataSet();
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
        }

        public Exception HandleException(Exception ex, SqlPreCommandSimple command)
        {
            var nex = ReplaceException(ex, command);
            nex.Data["Sql"] = command.PlainSql();
            return nex;
        }

        Exception ReplaceException(Exception ex, SqlPreCommandSimple command)
        {
            var se = ex as SqlException;
            if (se != null)
            {
                switch (se.Number)
                {
                    case -2: return new TimeoutException(ex.Message, ex);
                    case 2601: return new UniqueKeyException(ex);
                    case 547: return new ForeignKeyException(ex);
                    default: return ex;
                }
            }

            var ste = ex as SqlTypeException;
            if (ste != null && ex.Message.Contains("DateTime"))
            {
                var mins = command.Parameters.Where(a => DateTime.MinValue.Equals(a.Value));

                if (mins.Any())
                {
                    return new ArgumentOutOfRangeException("{0} {1} not initialized and equal to DateTime.MinValue".Formato(
                        mins.CommaAnd(a => a.ParameterName),
                        mins.Count() == 1 ? "is" : "are"), ex);
                }
            }

            return ex;

        }

        protected internal override void BulkCopy(DataTable dt, ObjectName destinationTable, SqlBulkCopyOptions options)
        {
            using (SqlConnection con = EnsureConnection())
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(
                options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction) ? con : (SqlConnection)Transaction.CurrentConnection,
                options,
                options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction) ? null : (SqlTransaction)Transaction.CurrentTransaccion))
            using (HeavyProfiler.Log("SQL", () => destinationTable.ToString() + " Rows:" + dt.Rows.Count))
            {
                foreach (DataColumn c in dt.Columns)
                    bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(c.ColumnName, c.ColumnName));

                bulkCopy.DestinationTableName = destinationTable.ToString();
                bulkCopy.WriteToServer(dt);
            }
        }

        public override string DatabaseName()
        {
            return new SqlConnection(connectionString).Database;
        }

        public override string DataSourceName()
        {
            return new SqlConnection(connectionString).DataSource;
        }

        public override void SaveTransactionPoint(DbTransaction transaction, string savePointName)
        {
            ((SqlTransaction)transaction).Save(savePointName);
        }

        public override void RollbackTransactionPoint(DbTransaction transaction, string savePointName)
        {
            ((SqlTransaction)transaction).Rollback(savePointName);
        }

        public override SqlDbType GetSqlDbType(DbParameter p)
        {
            return ((SqlParameter)p).SqlDbType;
        }

        public override DbParameter CloneParameter(DbParameter p)
        {
            SqlParameter sp = (SqlParameter)p;
            return new SqlParameter(sp.ParameterName, sp.Value) { IsNullable = sp.IsNullable, SqlDbType = sp.SqlDbType };
        }

        public override DbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public override ParameterBuilder ParameterBuilder { get; protected set; }

        public override void CleanDatabase(DatabaseName databaseName)
        {
            SqlConnectorScripts.RemoveAllScript(databaseName).ExecuteLeaves();
            SqlConnectorScripts.ShrinkDatabase(DatabaseName());
        }

        public override bool AllowsMultipleQueries
        {
            get { return true; }
        }

        public SqlConnector ForDatabase(Maps.DatabaseName database)
        {
            if (database == null)
                return this;

            return new SqlConnector(Replace(connectionString, database), this.Schema, this.DynamicQueryManager, this.Version);
        }

        private static string Replace(string connectionString, DatabaseName item)
        {
            var csb = new SqlConnectionStringBuilder(connectionString);
            csb.InitialCatalog = item.ToString();
            return csb.ToString();
        }

        public override bool AllowsSetSnapshotIsolation
        {
            get { return this.Version == SqlServerVersion.SqlServer2008; }
        }

        public override void FixType(ref SqlDbType type, ref int? size, ref int? scale)
        {
        }

        public override bool AllowsIndexWithWhere(string Where)
        {
            return Version > SqlServerVersion.SqlServer2005 && !ComplexWhereKeywords.Any(Where.Contains);
        }

        public static List<string> ComplexWhereKeywords = new List<string> { "OR" };

        public override SqlPreCommand ShringDatabase(string schemaName)
        {
            return new[]
            {
                this.Version == SqlServerVersion.SqlServer2005 ?  
                    new SqlPreCommandSimple("BACKUP LOG {0} WITH TRUNCATE_ONLY".Formato(schemaName)):
                    new []
                    {
                        new SqlPreCommandSimple("ALTER DATABASE {0} SET RECOVERY SIMPLE WITH NO_WAIT".Formato(schemaName)),
                        new[]{
                            new SqlPreCommandSimple("DECLARE @fileID BIGINT"),
                            new SqlPreCommandSimple("SET @fileID = (SELECT FILE_IDEX((SELECT TOP(1)name FROM sys.database_files WHERE type = 1)))"),
                            new SqlPreCommandSimple("DBCC SHRINKFILE(@fileID, 1)"),
                        }.Combine(Spacing.Simple).ToSimple(),
                        new SqlPreCommandSimple("ALTER DATABASE {0} SET RECOVERY FULL WITH NO_WAIT".Formato(schemaName)),                  
                    }.Combine(Spacing.Simple),
                new SqlPreCommandSimple("DBCC SHRINKDATABASE ( {0} , TRUNCATEONLY )".Formato(schemaName))
            }.Combine(Spacing.Simple);
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
    }

    public class SqlParameterBuilder : ParameterBuilder
    {
        public override DbParameter CreateParameter(string parameterName, SqlDbType sqlType, string udtTypeName, bool nullable, object value)
        {
            if (IsDate(sqlType))
                AssertDateTime((DateTime?)value);

            var result = new SqlParameter(parameterName, value == null ? DBNull.Value : value)
            {
                IsNullable = nullable
            };

            result.SqlDbType = sqlType;

            if (sqlType == SqlDbType.Udt)
                result.UdtTypeName = udtTypeName;


            return result;
        }

        public override MemberInitExpression ParameterFactory(Expression parameterName, SqlDbType sqlType, string udtTypeName, bool nullable, Expression value)
        {
            Expression valueExpr = Expression.Convert(IsDate(sqlType) ? Expression.Call(miAsserDateTime, value.Nullify()) : value, typeof(object));

            if (nullable)
                valueExpr = Expression.Condition(Expression.Equal(value, Expression.Constant(null, value.Type)),
                            Expression.Constant(DBNull.Value, typeof(object)),
                            valueExpr);

            NewExpression newExpr = Expression.New(typeof(SqlParameter).GetConstructor(new[] { typeof(string), typeof(object) }), parameterName, valueExpr);


            List<MemberBinding> mb = new List<MemberBinding>()
            {
                Expression.Bind(typeof(SqlParameter).GetProperty("IsNullable"), Expression.Constant(nullable)),
                Expression.Bind(typeof(SqlParameter).GetProperty("SqlDbType"), Expression.Constant(sqlType)),
            };

            if (sqlType == SqlDbType.Udt)
                mb.Add(Expression.Bind(typeof(SqlParameter).GetProperty("UdtTypeName"), Expression.Constant(udtTypeName)));

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

        public static SqlPreCommand RemoveAllScript(DatabaseName databaseName)
        {
            return SqlPreCommand.Combine(Spacing.Double,
                new SqlPreCommandSimple(Use(databaseName, RemoveAllProceduresScript)),
                new SqlPreCommandSimple(Use(databaseName, RemoveAllViewsScript)),
                new SqlPreCommandSimple(Use(databaseName, RemoveAllConstraintsScript)),
                new SqlPreCommandSimple(Use(databaseName, RemoveAllTablesScript)));
        }

        static string Use(DatabaseName databaseName, string script)
        {
            if (databaseName == null)
                return script;

            return "use " + databaseName + "\r\n" + script; 
        }

        internal static SqlPreCommand ShrinkDatabase(string schemaName)
        {
            return Connector.Current.ShringDatabase(schemaName);

        }
    }
}
