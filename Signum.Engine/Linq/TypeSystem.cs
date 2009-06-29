using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Linq.Expressions;
using System.Diagnostics;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// Type related helper methods
    /// </summary>
    internal static class ExpressionsUtilities
    {
        public static Expression TryConvert(this Expression expression, Type type)
        {
            if (!type.IsAssignableFrom(expression.Type))
                return Expression.Convert(expression, type);
            return expression;
        }

        public static Expression Nullify(this Expression expression)
        {
            if (!expression.Type.IsByRef)
                return Expression.Convert(expression, expression.Type.Nullify());
            return expression; 
        }

        [DebuggerStepThrough]
        public static Expression GetArgument(this MethodCallExpression mce, string parameterName)
        {
            int index = Array.FindIndex(mce.Method.GetParameters(), p => p.Name == parameterName);

            return mce.Arguments[index];
        }

        [DebuggerStepThrough]
        public static Expression TryGetArgument(this MethodCallExpression mce, string parameterName)
        {
            int index = Array.FindIndex(mce.Method.GetParameters(), p => p.Name == parameterName);

            return index == -1 ? null : mce.Arguments[index];
        }

        [DebuggerStepThrough]
        public static LambdaExpression StripQuotes(this Expression e)
        {
            if (e == null)
                return null;

            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return (LambdaExpression)e;
        }
    }
}
