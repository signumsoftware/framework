using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Utilities;
using System.Data.SqlServerCe;
using System.Data;
using Signum.Engine.Exceptions;
using System.Data.Common;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using System.IO;

namespace Signum.Engine
{
    public class SqlCeConnector : Connector
    {
        string connectionString;

        public SqlCeConnector(string connectionString, Schema schema, DynamicQueryManager dqm)
            : base(schema, dqm)
        {
            this.connectionString = connectionString;
            this.ParameterBuilder = new SqlCeParameterBuilder();
        }

        int? commandTimeout = null;
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


        public override bool SupportsScalarSubquery { get { return false; } }
        public override bool SupportsScalarSubqueryInAggregates { get { return false; } }

        SqlCeConnection EnsureConnection()
        {
            if (Transaction.HasTransaction)
                return null;

            SqlCeConnector current = ((SqlCeConnector)Connector.Current);
            SqlCeConnection result = new SqlCeConnection(current.ConnectionString);
            result.Open();
            return result;
        }

        SqlCeCommand NewCommand(SqlPreCommandSimple preCommand, SqlCeConnection overridenConnection)
        {
            SqlCeCommand cmd = new SqlCeCommand();

            int? timeout = Connector.ScopeTimeout ?? CommandTimeout;
            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            if (overridenConnection != null)
                cmd.Connection = overridenConnection;
            else
            {
                cmd.Connection = (SqlCeConnection)Transaction.CurrentConnection;
                cmd.Transaction = (SqlCeTransaction)Transaction.CurrentTransaccion;
            }

            cmd.CommandText = preCommand.Sql;

            if (preCommand.Parameters != null)
            {
                foreach (SqlCeParameter param in preCommand.Parameters)
                {
                    cmd.Parameters.Add(param);
                }
            }

            Log(preCommand);

            return cmd;
        }

        string selecctInsertedId = "SELECT CONVERT(Int,@@Identity) AS [newID]";
        string selectRowCount = "SELECT @@rowcount";

        protected internal override object ExecuteScalar(SqlPreCommandSimple preCommand)
        {
            using (SqlCeConnection con = EnsureConnection())
            using (SqlCeCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    if (cmd.CommandText.EndsWith(selecctInsertedId))
                    {
                        cmd.CommandText = cmd.CommandText.RemoveEnd(selecctInsertedId.Length);

                        cmd.ExecuteNonQuery();

                        cmd.CommandText = selecctInsertedId;

                        object result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                            return null;

                        return result;
                    }
                    else if (cmd.CommandText.EndsWith(selectRowCount))
                    {
                        cmd.CommandText = cmd.CommandText.RemoveEnd(selectRowCount.Length);

                        cmd.ExecuteNonQuery();

                        cmd.CommandText = selectRowCount;

                        object result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                            return null;

                        return result;
                    }
                    else
                    {
                        object result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                            return null;
                        return result;
                    }
                }
                catch (SqlCeException ex)
                {
                    var nex = HandleException(ex);
                    if (nex == ex)
                        throw;
                    throw nex;
                }
            }
        }

        protected internal override int ExecuteNonQuery(SqlPreCommandSimple preCommand)
        {
            using (SqlCeConnection con = EnsureConnection())
            using (SqlCeCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    int result = cmd.ExecuteNonQuery();
                    return result;
                }
                catch (SqlCeException ex)
                {
                    var nex = HandleException(ex);
                    if (nex == ex)
                        throw;
                    throw nex;
                }
            }
        }

        protected internal override void ExecuteDataReader(SqlPreCommandSimple command, Action<FieldReader> forEach)
        {
            using (SqlCeConnection con = EnsureConnection())
            using (SqlCeCommand cmd = NewCommand(command, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    using (SqlCeDataReader reader = cmd.ExecuteReader())
                    {
                        FieldReader fr = new FieldReader(reader);
                        int row = -1;
                        //try
                        //{
                        while (reader.Read())
                        {
                            row++;
                            forEach(fr);
                        }
                        //}
                        //catch (SqlTypeException ex)
                        //{
                        //    FieldReaderException fieldEx = fr.CreateFieldReaderException(ex);
                        //    fieldEx.Command = command;
                        //    fieldEx.Row = row;
                        //    throw fieldEx;
                        //}
                    }
                }
                catch (SqlCeException ex)
                {
                    var nex = HandleException(ex);
                    if (nex == ex)
                        throw;
                    throw nex;
                }

            }
        }

        protected internal override DbDataReader UnsafeExecuteDataReader(SqlPreCommandSimple preCommand)
        {
            try
            {
                SqlCeCommand cmd = NewCommand(preCommand, null);
                return cmd.ExecuteReader();
            }
            catch (SqlCeException ex)
            {
                var nex = HandleException(ex);
                if (nex == ex)
                    throw;
                throw nex;
            }
        }

        protected internal override DataTable ExecuteDataTable(SqlPreCommandSimple preCommand)
        {
            using (SqlCeConnection con = EnsureConnection())
            using (SqlCeCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    SqlCeDataAdapter da = new SqlCeDataAdapter(cmd);

                    DataTable result = new DataTable();
                    da.Fill(result);
                    return result;
                }
                catch (SqlCeException ex)
                {
                    var nex = HandleException(ex);
                    if (nex == ex)
                        throw;
                    throw nex;
                }
            }
        }

        protected internal override DataSet ExecuteDataSet(SqlPreCommandSimple preCommand)
        {
            using (SqlCeConnection con = EnsureConnection())
            using (SqlCeCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    SqlCeDataAdapter da = new SqlCeDataAdapter(cmd);
                    DataSet result = new DataSet();
                    da.Fill(result);
                    return result;
                }
                catch (SqlCeException ex)
                {
                    var nex = HandleException(ex);
                    if (nex == ex)
                        throw;
                    throw nex;
                }
            }
        }

        private Exception HandleException(SqlCeException ex)
        {
            switch (ex.NativeError)
            {
                case -2: return new TimeoutException(ex.Message, ex);
                case 2601: return new UniqueKeyException(ex);
                case 547: return new ForeignKeyException(ex);
                default: return ex;
            }
        }

        public override string DatabaseName()
        {
            return new SqlCeConnection(connectionString).Database;
        }

        public override string DataSourceName()
        {
            return new SqlCeConnection(connectionString).DataSource;
        }

        public override SqlDbType GetSqlDbType(System.Data.Common.DbParameter p)
        {
            return ((SqlCeParameter)p).SqlDbType;
        }

        public override void SaveTransactionPoint(System.Data.Common.DbTransaction transaction, string savePointName)
        {
            throw new InvalidOperationException("SqlCeTransaction does not suppor named transactions");
        }

        public override void RollbackTransactionPoint(System.Data.Common.DbTransaction Transaction, string savePointName)
        {
            throw new InvalidOperationException("SqlCeTransaction does not suppor named transactions");
        }

        public override System.Data.Common.DbParameter CloneParameter(System.Data.Common.DbParameter p)
        {
            throw new NotImplementedException();
        }

        public override DbConnection CreateConnection()
        {
            return new SqlCeConnection(ConnectionString);
        }

        public override ParameterBuilder ParameterBuilder { get; protected set; }

        public override void CleanDatabase()
        {
            string fileName = new SqlCeConnection(connectionString).DataSource;
            if (File.Exists(fileName))
                File.Delete(fileName);

            SqlCeEngine en = new SqlCeEngine(connectionString);
            en.CreateDatabase();
        }

        public override bool AllowsMultipleQueries
        {
            get { return false; }
        }
    }

    public class SqlCeParameterBuilder : ParameterBuilder
    {
        public override DbParameter CreateParameter(string parameterName, SqlDbType sqlType, string udtTypeName, bool nullable, object value)
        {
            if (IsDate(sqlType))
                AssertDateTime((DateTime?)value);

            var result = new SqlCeParameter(parameterName, value == null ? DBNull.Value : value)
            {
                IsNullable = nullable
            };

            result.SqlDbType = sqlType;

            if (sqlType == SqlDbType.Udt)
                throw new InvalidOperationException("User Defined Tyeps not supported on SQL Server Compact ({0})".Formato(udtTypeName));

            return result;
        }

        public override MemberInitExpression ParameterFactory(Expression parameterName, SqlDbType sqlType, string udtTypeName, bool nullable, Expression value)
        {
            Expression valueExpr = Expression.Convert(IsDate(sqlType) ? Expression.Call(miAsserDateTime, value.Nullify()) : value, typeof(object));

            if (nullable)
                valueExpr = Expression.Condition(Expression.Equal(value, Expression.Constant(null, value.Type)),
                            Expression.Constant(DBNull.Value, typeof(object)),
                            valueExpr);

            NewExpression newExpr = Expression.New(typeof(SqlCeParameter).GetConstructor(new[] { typeof(string), typeof(object) }), parameterName, valueExpr);


            List<MemberBinding> mb = new List<MemberBinding>()
            {
                Expression.Bind(typeof(SqlCeParameter).GetProperty("IsNullable"), Expression.Constant(nullable)),
                Expression.Bind(typeof(SqlCeParameter).GetProperty("SqlDbType"), Expression.Constant(sqlType)),
            };

            if (sqlType == SqlDbType.Udt)
                throw new InvalidOperationException("User Defined Tyeps not supported on SQL Server Compact ({0})".Formato(udtTypeName));

            return Expression.MemberInit(newExpr, mb);
        }
    }
}
