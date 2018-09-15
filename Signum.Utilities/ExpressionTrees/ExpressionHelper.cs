using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;

namespace Signum.Utilities.ExpressionTrees
{
    
    public static class ExpressionHelper
    {
        static MethodInfo miAsQueryable = ReflectionTools.GetMethodInfo(() => Queryable.AsQueryable<int>(null)).GetGenericMethodDefinition();

        [DebuggerStepThrough]
        public static Expression TryConvert(this Expression expression, Type type)
        {
            if (!type.IsAssignableFrom(expression.Type))
                return Expression.Convert(expression, type);
            return expression;
        }

        [DebuggerStepThrough]
        public static Expression TryRemoveConvert(this Expression expression, Func<Type, bool> isAllowed)
        {

            if (expression.NodeType == ExpressionType.Convert && isAllowed(expression.Type))
                return ((UnaryExpression)expression).Operand;

            return null;
        }

        [DebuggerStepThrough]
        public static Expression Nullify(this Expression expression)
        {
            Type type = expression.Type.Nullify();
            if (expression.Type != type)
                return Expression.Convert(expression, type);
            return expression;
        }

        [DebuggerStepThrough]
        public static Expression UnNullify(this Expression expression)
        {
            Type type = expression.Type.UnNullify();
            if (expression.Type != type)
                return Expression.Convert(expression, type);
            return expression;
        }


        [DebuggerStepThrough]
        public static Expression RemoveNullify(this Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert && expression.Type == ((UnaryExpression)expression).Operand.Type.Nullify())
                return ((UnaryExpression)expression).Operand;
            return expression;
        }

        [DebuggerStepThrough]
        public static Expression RemoveUnNullify(this Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert && expression.Type == ((UnaryExpression)expression).Operand.Type.UnNullify())
                return ((UnaryExpression)expression).Operand;

            if (expression.NodeType == ExpressionType.MemberAccess && ((MemberExpression)expression).Member.Name == "Value" && expression.Type == ((MemberExpression)expression).Expression.Type.UnNullify())
                return ((MemberExpression)expression).Expression;

            return expression;
        }

        public static Expression AggregateAnd(this IEnumerable<Expression> expressions)
        {
            var enumerator = expressions.GetEnumerator();

            if(!enumerator.MoveNext())
                return Expression.Constant(true);

            Expression acum = enumerator.Current;

            while(enumerator.MoveNext())
                acum = Expression.And(acum, enumerator.Current);

            return acum;
        }

        public static Expression AggregateOr(this IEnumerable<Expression> expressions)
        {
            var enumerator = expressions.GetEnumerator();

            if (!enumerator.MoveNext())
                return Expression.Constant(false);

            Expression acum = enumerator.Current;

            while (enumerator.MoveNext())
                acum = Expression.Or(acum, enumerator.Current);

            return acum;
        }

        [DebuggerStepThrough]
        public static Expression GetArgument(this MethodCallExpression mce, string parameterName)
        {
            int index = FindParameter(mce.Method.GetParameters(), parameterName);

            if (index == -1)
                throw new ArgumentException("parameterName '{0}' not found".FormatWith(parameterName));

            return mce.Arguments[index];
        }

        [DebuggerStepThrough]
        public static Expression TryGetArgument(this MethodCallExpression mce, string parameterName)
        {
            int index = FindParameter(mce.Method.GetParameters(), parameterName);

            return index == -1 ? null : mce.Arguments[index];
        }

        [DebuggerStepThrough]
        private static int FindParameter(ParameterInfo[] parameters, string parameterName)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Name == parameterName)
                    return i;
            }

            return -1;
        }
      

        [DebuggerStepThrough]
        public static LambdaExpression StripQuotes(this Expression e)
        {
            if (e == null)
                return null;

            if (e is ConstantExpression)
                return (LambdaExpression)((ConstantExpression)e).Value;

            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return (LambdaExpression)e;
        }

        [DebuggerStepThrough]
        public static bool IsBase(this IQueryable query)
        {
            ConstantExpression ce = query.Expression as ConstantExpression;
            return ce != null && ce.Value == query; 
        }

        public static string QueryText<T>(this IQueryable<T> query)
        {
            var q = query as Query<T>;

            if (q == null)
                throw new ArgumentException("query is not an instance of {0}".FormatWith(typeof(Query<T>).NicePluralName()));

            return q.QueryText;
        }
    }
}
