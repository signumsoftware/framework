using System.Collections.Generic;
using System.Data;
using System.Linq;
using Signum.Engine;
using System.IO;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace Signum.Engine
{
    public static class Executor
    {
        public static object ExecuteScalar(string sql)
        {
            return Connector.Current.ExecuteScalar(new SqlPreCommandSimple(sql));
        }

        public static object ExecuteScalar(string sql, List<DbParameter> parameters)
        {
            return Connector.Current.ExecuteScalar(new SqlPreCommandSimple(sql, parameters));
        }

        public static object ExecuteScalar(this SqlPreCommandSimple preCommand)
        {
            return Connector.Current.ExecuteScalar(preCommand);
        }


        public static int ExecuteNonQuery(string sql)
        {
            return Connector.Current.ExecuteNonQuery(new SqlPreCommandSimple(sql));
        }

        public static int ExecuteNonQuery(string sql, List<DbParameter> parameters)
        {
            return Connector.Current.ExecuteNonQuery(new SqlPreCommandSimple(sql, parameters));
        }

        public static int ExecuteNonQuery(this SqlPreCommandSimple preCommand)
        {
            return Connector.Current.ExecuteNonQuery(preCommand);
        }

        public static DbDataReader UnsafeExecuteDataReader(string sql)
        {
            return Connector.Current.UnsafeExecuteDataReader(new SqlPreCommandSimple(sql));
        }

        public static DbDataReader UnsafeExecuteDataReader(string sql, List<DbParameter> parameters)
        {
            return Connector.Current.UnsafeExecuteDataReader(new SqlPreCommandSimple(sql, parameters));
        }

        public static DbDataReader UnsafeExecuteDataReader(this SqlPreCommandSimple preCommand)
        {
            return Connector.Current.UnsafeExecuteDataReader(preCommand);
        }


        public static DataTable ExecuteDataTable(string sql)
        {
            return Connector.Current.ExecuteDataTable(new SqlPreCommandSimple(sql));
        }

        public static DataTable ExecuteDataTable(string sql, List<DbParameter> parameters)
        {
            return Connector.Current.ExecuteDataTable(new SqlPreCommandSimple(sql, parameters));
        }

        public static DataTable ExecuteDataTable(this SqlPreCommandSimple preCommand)
        {
            return Connector.Current.ExecuteDataTable(preCommand);
        }

        public static DataSet ExecuteDataSet(string sql)
        {
            return Connector.Current.ExecuteDataSet(new SqlPreCommandSimple(sql));
        }

        public static DataSet ExecuteDataSet(string sql, List<DbParameter> parameters)
        {
            return Connector.Current.ExecuteDataSet(new SqlPreCommandSimple(sql, parameters));
        }

        public static DataSet ExecuteDataSet(this SqlPreCommandSimple preCommand)
        {
            return Connector.Current.ExecuteDataSet(preCommand);
        }

        public static void ExecuteLeaves(this SqlPreCommand preCommand)
        {
            foreach (var simple in preCommand.Leaves())
            {
                simple.ExecuteNonQuery();
            }
        }
    }
}