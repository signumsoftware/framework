using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Signum.Utilities;
using Signum.Engine.Maps;
using System.IO;
using System.Data.SqlClient;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine
{
    public abstract class BaseConnection
    {
        public BaseConnection(Schema schema)
        {
            this.schema = schema;
        }

        Schema schema = new Schema();
        public Schema Schema
        {
            get { return schema; }
            set { schema = value; }
        }

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

        protected internal abstract bool IsMock { get; }

        protected internal abstract object ExecuteScalar(SqlPreCommandSimple preCommand);
        protected internal abstract int ExecuteNonQuery(SqlPreCommandSimple preCommand);
        protected internal abstract DataTable ExecuteDataTable(SqlPreCommandSimple command);
        protected internal abstract DataSet ExecuteDataSet(SqlPreCommandSimple sqlPreCommandSimple);
    }


    public class Connection : BaseConnection
    {
        int? commandTimeout = null;
        IsolationLevel isolationLevel = IsolationLevel.Unspecified;
        string connectionString;

        public Connection(string connectionString, Schema schema)
            : base(schema)
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

        protected internal override bool IsMock
        {
            get { return false; }
        }

        SqlCommand NewCommand(SqlPreCommandSimple preCommand)
        {
            SqlCommand cmd = new SqlCommand();

            int? timeout = CommandTimeoutScope.Current ?? CommandTimeout;
            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            cmd.Connection = Transaction.CurrentConnection;
            cmd.Transaction = Transaction.CurrentTransaccion;
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
            using (Transaction tr = new Transaction())
            using (SqlCommand cmd = NewCommand(preCommand))
            {
                object result = cmd.ExecuteScalar();
                tr.Commit();
                return result;
            }
        }

        protected internal override int ExecuteNonQuery(SqlPreCommandSimple preCommand)
        {
            using (Transaction tr = new Transaction())
            using (SqlCommand cmd = NewCommand(preCommand))
            {
                int result = cmd.ExecuteNonQuery();
                tr.Commit();
                return result;
            }
        }

        protected internal override DataTable ExecuteDataTable(SqlPreCommandSimple preCommand)
        {
            using (Transaction tr = new Transaction())
            using (SqlCommand cmd = NewCommand(preCommand))
            {
                SqlDataAdapter da = new SqlDataAdapter(cmd);

                DataTable result = new DataTable();
                da.Fill(result);

                tr.Commit();
                return result;
            }
        }

        protected internal override DataSet ExecuteDataSet(SqlPreCommandSimple preCommand)
        {
            using (Transaction tr = new Transaction())
            using (SqlCommand cmd = NewCommand(preCommand))
            {
                SqlDataAdapter da = new SqlDataAdapter(cmd);

                DataSet result = new DataSet();
                da.Fill(result);

                tr.Commit();
                return result;
            }
        }
    }

    public class MockConnection : BaseConnection
    {
        public MockConnection(Schema schema)
            : base(schema)
        {

        }

        List<SqlPreCommandSimple> executedCommands = new List<SqlPreCommandSimple>();
        public List<SqlPreCommandSimple> ExecutedCommands
        {
            get { return executedCommands; }
        }

        protected internal override bool IsMock
        {
            get { return true; }
        }

        protected internal override object ExecuteScalar(SqlPreCommandSimple preCommand)
        {
            executedCommands.Add(preCommand);
            return null;
        }

        protected internal override int ExecuteNonQuery(SqlPreCommandSimple preCommand)
        {
            executedCommands.Add(preCommand);
            return 0;
        }

        protected internal override DataTable ExecuteDataTable(SqlPreCommandSimple preCommand)
        {
            executedCommands.Add(preCommand);
            return null;
        }

        protected internal override DataSet ExecuteDataSet(SqlPreCommandSimple preCommand)
        {
            executedCommands.Add(preCommand);
            return null;
        }
    }
}
