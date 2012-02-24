using System;
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

namespace Signum.Engine
{
    public class SqlConnector : Connector
    {
        int? commandTimeout = null;
        string connectionString;

        public SqlConnector(string connectionString, Schema schema, DynamicQueryManager dqm)
            : base(schema, dqm)
        {
            this.connectionString = connectionString;
            this.ParameterBuilder = new SqlParameterBuilder();
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

        SqlConnection EnsureConnection()
        {
            if (Transaction.HasTransaction)
                return null;

            SqlConnector current = ((SqlConnector)Connector.Current);
            SqlConnection result = new SqlConnection(current.ConnectionString);
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
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    object result = cmd.ExecuteScalar();
                    return result;
                }
                catch (SqlException ex)
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
            using (SqlConnection con = EnsureConnection())
            using (SqlCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    int result = cmd.ExecuteNonQuery();
                    return result;
                }
                catch (SqlException ex)
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
            using (SqlConnection con = EnsureConnection())
            using (SqlCommand cmd = NewCommand(command, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
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
                        catch (SqlTypeException ex)
                        {
                            FieldReaderException fieldEx = fr.CreateFieldReaderException(ex);
                            fieldEx.Command = command;
                            fieldEx.Row = row;
                            throw fieldEx;
                        }
                    }
                }
                catch (SqlException ex)
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
                SqlCommand cmd = NewCommand(preCommand, null);
                return cmd.ExecuteReader();
            }
            catch (SqlException ex)
            {
                var nex = HandleException(ex);
                if (nex == ex)
                    throw;
                throw nex;
            }
        }

        protected internal override DataTable ExecuteDataTable(SqlPreCommandSimple preCommand)
        {
            using (SqlConnection con = EnsureConnection())
            using (SqlCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(cmd);

                    DataTable result = new DataTable();
                    da.Fill(result);
                    return result;
                }
                catch (SqlException ex)
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
            using (SqlConnection con = EnsureConnection())
            using (SqlCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet result = new DataSet();
                    da.Fill(result);
                    return result;
                }
                catch (SqlException ex)
                {
                    var nex = HandleException(ex);
                    if (nex == ex)
                        throw;
                    throw nex;
                }
            }
        }

        private Exception HandleException(SqlException ex)
        {
            switch (ex.Number)
            {
                case -2: return new TimeoutException(ex.Message, ex);
                case 2601: return new UniqueKeyException(ex);
                case 547: return new ForeignKeyException(ex);
                default: return ex;
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
    }

    public class SqlParameterBuilder : ParameterBuilder
    {
        public override string GetParameterName(string name)
        {
            return "@" + name;
        }

        public override DbParameter CreateReferenceParameter(string name, bool nullable, int? id)
        {
            return CreateParameter(name, SqlBuilder.PrimaryKeyType, nullable, id);
        }

        public override DbParameter CreateParameter(string name, object value, Type type)
        {
            return CreateParameter(name,
             Schema.Current.Settings.DefaultSqlType(type.UnNullify()),
             type == null || type.IsByRef || type.IsNullable(),
             value);
        }

        public override DbParameter CreateParameter(string name, SqlDbType type, bool nullable, object value)
        {
            if (IsDate(type))
                AssertDateTime((DateTime?)value);

            return new SqlParameter(GetParameterName(name), type)
            {
                IsNullable = nullable,
                Value = value == null ? DBNull.Value : value,
                SourceColumn = name,
            };
        }

        public override MemberInitExpression ParameterFactory(Expression name, SqlDbType type, bool nullable, Expression value)
        {
            NewExpression newParam = Expression.New(typeof(SqlParameter).GetConstructor(new[] { typeof(string), typeof(SqlDbType) }), name, Expression.Constant(type));
            
            Expression valueExpr = Expression.Convert(IsDate(type) ? Expression.Call(miAsserDateTime, value.Nullify()) : value, typeof(object));

            if (nullable)
                return Expression.MemberInit(newParam, new MemberBinding[]
                {
                    Expression.Bind(typeof(SqlParameter).GetProperty("IsNullable"), Expression.Constant(true)),
                    Expression.Bind(typeof(SqlParameter).GetProperty("Value"), 
                        Expression.Condition(Expression.Equal(value, Expression.Constant(null, value.Type)), 
                            Expression.Constant(DBNull.Value, typeof(object)),
                            valueExpr))
                });
            else
                return Expression.MemberInit(newParam, new MemberBinding[]
                {  
                    Expression.Bind(typeof(SqlParameter).GetProperty("Value"), valueExpr)
                });
        }



        public override DbParameter UnsafeCreateParameter(string name, SqlDbType type, bool nullable, object value)
        {
            if (IsDate(type))
                AssertDateTime((DateTime?)value);

            return new SqlParameter(name, type)
            {
                IsNullable = nullable,
                Value = value == null ? DBNull.Value : value,
            };
        }
    }
}
