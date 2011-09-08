using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Signum.Utilities;
using System.Data.SqlClient;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Entities;

namespace Signum.Engine
{
    public static class SqlParameterBuilder
    {
        [ThreadStatic]
        static int parameterCounter;

        static string GetParameterName(string name)
        {
            return "@" + name + (parameterCounter++).ToString();
        }

        public static SqlParameter CreateReferenceParameter(string name, bool nullable, int? id)
        {
            return CreateParameter(name, SqlBuilder.PrimaryKeyType, nullable, id);
        }

        public static SqlParameter CreateIdParameter(int id)
        {
            return CreateParameter(SqlBuilder.PrimaryKeyName, SqlBuilder.PrimaryKeyType, false, id);
        }

        public static SqlParameter CreateParameter(string name, SqlDbType type, bool nullable, object value)
        {
            AssertDateTime(value);

            return new SqlParameter(GetParameterName(name), type)
            {
                IsNullable = nullable,
                Value = value == null ? DBNull.Value : value,
                SourceColumn = name,
            };
        }

        public static SqlParameter UnsafeCreateParameter(string name, SqlDbType type, bool nullable, object value)
        {
            AssertDateTime(value);

            return new SqlParameter(name, type)
            {
                IsNullable = nullable,
                Value = value == null ? DBNull.Value : value,
            };
        }

        static void AssertDateTime(object value)
        {
            if (Schema.Current.TimeZoneMode == TimeZoneMode.Utc && value is DateTime && ((DateTime)value).Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("Attempt to use a non-Utc date in the database");
        }
    }
 
}
