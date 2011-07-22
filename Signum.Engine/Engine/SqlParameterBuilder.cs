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
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine
{
    public static class SqlParameterBuilder
    {
        public static string GetParameterName(string name)
        {
            return "@" + name;
        }

        public static SqlParameter CreateReferenceParameter(string name, bool nullable, int? id)
        {
            return CreateParameter(name, SqlBuilder.PrimaryKeyType, nullable, id);
        }

        public static SqlParameter CreateParameter(string name, object value, Type type)
        {
            return CreateParameter(name,
             Schema.Current.Settings.DefaultSqlType(type.UnNullify()),
             type == null || type.IsByRef || type.IsNullable(), 
             value);
        }

        public static SqlParameter CreateParameter(string name, SqlDbType type, bool nullable, object value)
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

        static MethodInfo miAsserDateTime = ReflectionTools.GetMethodInfo(() => AssertDateTime(null));

        public static MemberInitExpression ParameterFactory(Expression name, SqlDbType type, bool nullable, Expression value)
        {
            NewExpression newParam = Expression.New(typeof(SqlParameter).GetConstructor(new []{typeof(string), typeof(SqlDbType)}), name, Expression.Constant(type));

            Expression valueExpr = Expression.Convert(IsDate(type)?Expression.Call(miAsserDateTime, value.Nullify()): value, typeof(object));

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

        static bool IsDate(SqlDbType type)
        {
            return type == SqlDbType.Date || type == SqlDbType.DateTime || type == SqlDbType.DateTime2 || type == SqlDbType.SmallDateTime;
        }

        public static SqlParameter UnsafeCreateParameter(string name, SqlDbType type, bool nullable, object value)
        {
            if (IsDate(type))
                AssertDateTime((DateTime?)value);

            return new SqlParameter(name, type)
            {
                IsNullable = nullable,
                Value = value == null ? DBNull.Value : value,
            };
        }

        static DateTime? AssertDateTime(DateTime? dateTime)
        {
            if (Schema.Current.TimeZoneMode == TimeZoneMode.Utc && dateTime.HasValue && dateTime.Value.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("Attempt to use a non-Utc date in the database");

            return dateTime;
        }
    }
 
}
