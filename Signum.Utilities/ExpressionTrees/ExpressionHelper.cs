using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Signum.Utilities.ExpressionTrees
{
    
    public static class ExpressionHelper
    {
        [DebuggerStepThrough]
        public static ReadOnlyCollection<T> NewIfChange<T>( this ReadOnlyCollection<T> collection, Func<T,T> newValue)
            where T:class
        {
            if (collection == null)
                return null; 

            List<T> alternate = null;
            for (int i = 0, n = collection.Count; i < n; i++)
            {
                T item = collection[i];
                T newItem = newValue(item);
                if (alternate == null && item != newItem)
                {
                    alternate = collection.Take(i).ToList();
                }
                if (alternate != null && newItem != null)
                {
                    alternate.Add(newItem);
                }
            }
            if (alternate != null)
            {
                return alternate.AsReadOnly();
            }
            return collection;
        }

        [DebuggerStepThrough]
        public static List<T> NewIfChange<T>(this List<T> collection, Func<T, T> newValue)
          where T : class
        {
            if (collection == null)
                return null;

            List<T> alternate = null;
            for (int i = 0, n = collection.Count; i < n; i++)
            {
                T item = collection[i];
                T newItem = newValue(item);
                if (alternate == null && item != newItem)
                {
                    alternate = collection.Take(i).ToList();
                }
                if (alternate != null && newItem != null)
                {
                    alternate.Add(newItem);
                }
            }
            return alternate ?? collection; 
        }

        [DebuggerStepThrough]
        public static Expression TryConvert(this Expression expression, Type type)
        {
            if (!type.IsAssignableFrom(expression.Type))
                return Expression.Convert(expression, type);
            return expression;
        }

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
        public static bool IsBase(this IQueryable query)
        {
            ConstantExpression ce = query.Expression as ConstantExpression;
            return ce != null && ce.Value == query; 
        }
    }
}
