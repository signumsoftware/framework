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
using Signum.Engine.Exceptions;
using Signum.Engine.DynamicQuery;
using System.Data.SqlTypes;

namespace Signum.Engine
{
    public enum DBMS
    {
        SqlServer2005, 
        SqlServer2008
    }

    public abstract class BaseConnection
    {
        public BaseConnection(Schema schema, DynamicQueryManager dqm)
        {
            this.Schema = schema;
            this.DynamicQueryManager = dqm;
        }

        public DBMS DBMS { get; set; }

        public Schema Schema { get; private set; }
        public DynamicQueryManager DynamicQueryManager { get; private set; }

        [ThreadStatic]
        static TextWriter log;
        public static TextWriter CurrentLog
        {
            get { return log; }
            set { log = value; }
        }

        protected static void Log(SqlPreCommandSimple pcs)
        {
            if (log != null)
            {
                log.WriteLine(pcs.Sql);
                if (pcs.Parameters != null)
                    log.WriteLine(pcs.Parameters
                        .ToString(p => "{0} {1}: {2}".Formato(
                            p.ParameterName,
                            p.SqlDbType,
                            p.Value.TryCC(v => CSharpRenderer.Value(v, v.GetType(), null))), "\r\n"));
                log.WriteLine();
            }
        }

        protected internal abstract object ExecuteScalar(SqlPreCommandSimple preCommand);
        protected internal abstract int ExecuteNonQuery(SqlPreCommandSimple preCommand);
        protected internal abstract DataTable ExecuteDataTable(SqlPreCommandSimple command);
        protected internal abstract void ExecuteDataReader(SqlPreCommandSimple command, Action<FieldReader> forEach);
        protected internal abstract SqlDataReader UnsafeExecuteDataReader(SqlPreCommandSimple sqlPreCommandSimple);
        protected internal abstract DataSet ExecuteDataSet(SqlPreCommandSimple sqlPreCommandSimple);

        public abstract string DatabaseName();

        public abstract string DataSourceName();

        public virtual int MaxNameLength { get { return 128; } }
    }


    public class Connection : BaseConnection
    {
        int? commandTimeout = null;
        IsolationLevel isolationLevel = IsolationLevel.Unspecified;
        string connectionString;

        public Connection(string connectionString, Schema schema, DynamicQueryManager dqm)
            : base(schema, dqm)
        {
            this.connectionString = connectionString;
        }

        public IsolationLevel IsolationLevel
        {
            get { return isolationLevel; }
            set { isolationLevel = value; }
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

        [ThreadStatic]
        public static long CommandCount = 0;

        SqlConnection EnsureConnection()
        {
            if (Transaction.HasTransaction)
                return null;

            Connection current = ((Connection)ConnectionScope.Current);
            SqlConnection result = new SqlConnection(current.ConnectionString);
            result.Open();
            return result;
        }

        SqlCommand NewCommand(SqlPreCommandSimple preCommand, SqlConnection overridenConnection)
        {
            SqlCommand cmd = new SqlCommand();

            int? timeout = CommandTimeoutScope.Current ?? CommandTimeout;
            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            if (overridenConnection != null)
                cmd.Connection = overridenConnection;
            else
            {
                cmd.Connection = Transaction.CurrentConnection;
                cmd.Transaction = Transaction.CurrentTransaccion;
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
                    CommandCount++;
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
                    CommandCount++;
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

        protected internal override SqlDataReader UnsafeExecuteDataReader(SqlPreCommandSimple preCommand)
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
                    CommandCount++;
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
                    CommandCount++;
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
    }
}
