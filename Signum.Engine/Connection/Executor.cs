using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Signum.Engine;
using System.IO;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using System;

namespace Signum.Engine
{
public static class Executor
{
    public static object ExecuteScalar(string sql)
    {
        return ConnectionScope.Current.ExecuteScalar(new SqlPreCommandSimple(sql));
    }

    public static object ExecuteScalar(string sql, List<SqlParameter> parameters)
    {
        return ConnectionScope.Current.ExecuteScalar(new SqlPreCommandSimple(sql, parameters));
    }

    public static object ExecuteScalar(this SqlPreCommandSimple preCommand)
    {
        return ConnectionScope.Current.ExecuteScalar(preCommand);
    }


    public static int ExecuteNonQuery(string sql)
    {
        return ConnectionScope.Current.ExecuteNonQuery(new SqlPreCommandSimple(sql));
    }

    public static int ExecuteNonQuery(string sql, List<SqlParameter> parameters)
    {
        return ConnectionScope.Current.ExecuteNonQuery(new SqlPreCommandSimple(sql, parameters));
    }

    public static int ExecuteNonQuery(this SqlPreCommandSimple preCommand)
    {
        return ConnectionScope.Current.ExecuteNonQuery(preCommand);
    }


    public static DataTable ExecuteDataTable(string sql)
    {
        return ConnectionScope.Current.ExecuteDataTable(new SqlPreCommandSimple(sql));
    }

    public static DataTable ExecuteDataTable(string sql, List<SqlParameter> parameters)
    {
        return ConnectionScope.Current.ExecuteDataTable(new SqlPreCommandSimple(sql, parameters));
    }

    public static DataTable ExecuteDataTable(this SqlPreCommandSimple preCommand)
    {
        return ConnectionScope.Current.ExecuteDataTable(preCommand);
    }

    public static DataSet ExecuteDataSet(string sql)
    {
        return ConnectionScope.Current.ExecuteDataSet(new SqlPreCommandSimple(sql));
    }

    public static DataSet ExecuteDataSet(string sql, List<SqlParameter> parameters)
    {
        return ConnectionScope.Current.ExecuteDataSet(new SqlPreCommandSimple(sql, parameters));
    }

    public static DataSet ExecuteDataSet(this SqlPreCommandSimple preCommand)
    {
        return ConnectionScope.Current.ExecuteDataSet(preCommand);
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