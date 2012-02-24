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

namespace Signum.Engine
{
    class SqlCeConnector : Connector
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

        static readonly Variable<int?> scopeTimeout = Statics.ThreadVariable<int?>("scopeTimeout");
        public static IDisposable CommandTimeoutScope(int? timeout)
        {
            var old = scopeTimeout.Value;
            scopeTimeout.Value = timeout;
            return new Disposable(() => scopeTimeout.Value = timeout);
        }

        public string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value; }
        }

        SqlCeConnection EnsureConnection()
        {
            if (Transaction.HasTransaction)
                return null;

            SqlConnector current = ((SqlConnector)Connector.Current);
            SqlCeConnection result = new SqlCeConnection(current.ConnectionString);
            result.Open();
            return result;
        }

        SqlCeCommand NewCommand(SqlPreCommandSimple preCommand, SqlCeConnection overridenConnection)
        {
            SqlCeCommand cmd = new SqlCeCommand();

            int? timeout = scopeTimeout.Value ?? CommandTimeout;
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

        protected internal override object ExecuteScalar(SqlPreCommandSimple preCommand)
        {
            using (SqlCeConnection con = EnsureConnection())
            using (SqlCeCommand cmd = NewCommand(preCommand, con))
            using (HeavyProfiler.Log("SQL", cmd.CommandText))
            {
                try
                {
                    object result = cmd.ExecuteScalar();
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
    }

    public class SqlCeParameterBuilder : ParameterBuilder
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

            return new SqlCeParameter(GetParameterName(name), type)
            {
                IsNullable = nullable,
                Value = value == null ? DBNull.Value : value,
                SourceColumn = name,
            };
        }

        public override MemberInitExpression ParameterFactory(Expression name, SqlDbType type, bool nullable, Expression value)
        {
            NewExpression newParam = Expression.New(typeof(SqlCeParameter).GetConstructor(new[] { typeof(string), typeof(SqlDbType) }), name, Expression.Constant(type));
            
            Expression valueExpr = Expression.Convert(IsDate(type) ? Expression.Call(miAsserDateTime, value.Nullify()) : value, typeof(object));

            if (nullable)
                return Expression.MemberInit(newParam, new MemberBinding[]
                {
                    Expression.Bind(typeof(SqlCeParameter).GetProperty("IsNullable"), Expression.Constant(true)),
                    Expression.Bind(typeof(SqlCeParameter).GetProperty("Value"), 
                        Expression.Condition(Expression.Equal(value, Expression.Constant(null, value.Type)), 
                            Expression.Constant(DBNull.Value, typeof(object)),
                            valueExpr))
                });
            else
                return Expression.MemberInit(newParam, new MemberBinding[]
                {  
                    Expression.Bind(typeof(SqlCeParameter).GetProperty("Value"), valueExpr)
                });
        }



        public override DbParameter UnsafeCreateParameter(string name, SqlDbType type, bool nullable, object value)
        {
            if (IsDate(type))
                AssertDateTime((DateTime?)value);

            return new SqlCeParameter(name, type)
            {
                IsNullable = nullable,
                Value = value == null ? DBNull.Value : value,
            };
        }
    }
}
