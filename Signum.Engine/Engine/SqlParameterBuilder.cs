using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Signum.Utilities;
using System.Data.SqlClient;
using Signum.Engine;

namespace Signum.Engine
{
    internal static class SqlParameterBuilder
    {
        [ThreadStatic]
        static int contadorParametro;
        public static string GetParameterName(string name)
        {
            return "@" + name + (contadorParametro++).ToString();
        }

        public static SqlParameter CreateReferenceParameter(string name, bool nullable, int? id)
        {
            return CreateParameter(name, SqlBuilder.PrimaryKeyType, nullable, id);
        }

        public static SqlParameter CreateParameter(string name, SqlDbType type, bool nullable, object value)
        {
            SqlParameter param = new SqlParameter(GetParameterName(name), type)
            {
                IsNullable = nullable,
                Value = value == null ? DBNull.Value : value,
                SourceColumn = name,
            };

            return param; 
        }
    }
 
}
