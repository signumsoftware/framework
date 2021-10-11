using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Signum.Engine.Maps;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine
{
    public static class Executor
    {
        public static object? ExecuteScalar(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return Connector.Current.ExecuteScalar(new SqlPreCommandSimple(sql, parameters), commandType);
        }

        public static object? ExecuteScalar(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text)
        {
            return Connector.Current.ExecuteScalar(preCommand, commandType);
        }
        
        public static int ExecuteNonQuery(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return Connector.Current.ExecuteNonQuery(new SqlPreCommandSimple(sql, parameters), commandType);
        }

        public static int ExecuteNonQuery(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text)
        {
            return Connector.Current.ExecuteNonQuery(preCommand, commandType);
        }


        public static DbDataReaderWithCommand UnsafeExecuteDataReader(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return Connector.Current.UnsafeExecuteDataReader(new SqlPreCommandSimple(sql, parameters), commandType);
        }

        public static DbDataReaderWithCommand UnsafeExecuteDataReader(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text)
        {
            return Connector.Current.UnsafeExecuteDataReader(preCommand, commandType);
        }

        public static Task<DbDataReaderWithCommand> UnsafeExecuteDataReaderAsync(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text, CancellationToken token = default)
        {
            return Connector.Current.UnsafeExecuteDataReaderAsync(new SqlPreCommandSimple(sql, parameters), commandType, token);
        }

        public static Task<DbDataReaderWithCommand> UnsafeExecuteDataReaderAsync(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text, CancellationToken token = default)
        {
            return Connector.Current.UnsafeExecuteDataReaderAsync(preCommand, commandType, token);
        }

        public static DataTable ExecuteDataTable(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return Connector.Current.ExecuteDataTable(new SqlPreCommandSimple(sql, parameters), commandType);
        }

        public static DataTable ExecuteDataTable(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text)
        {
            return Connector.Current.ExecuteDataTable(preCommand, commandType);
        }

        public static void ExecuteLeaves(this SqlPreCommand preCommand, CommandType commandType = CommandType.Text)
        {
            foreach (var simple in preCommand.Leaves())
            {
                simple.ExecuteNonQuery(commandType);
            }
        }

        public static void BulkCopy(DataTable dt, List<IColumn> column, ObjectName destinationTable, SqlBulkCopyOptions options, int? timeout)
        {
            Connector.Current.BulkCopy(dt, column, destinationTable, options, timeout);
        }
    }
}
