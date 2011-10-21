using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Utilities
{
    /// <summary>
    /// Contains extension methods for Expression class. These methods
    /// can be used to 'call' expression tree and can be translated to IQueryable
    /// </summary>
    public static class ExpressionExtensions
    {
        static Dictionary<LambdaExpression, Delegate> cache = new Dictionary<LambdaExpression, Delegate>();

        public static T CompileAndStore<T>(this Expression<T> expression)
        {
            lock (cache)
                return (T)(object)cache.GetOrCreate(expression, () => (Delegate)(object)expression.Compile());
        }


        /// <summary>
        /// Invoke expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Invoke<T>(this Expression<Func<T>> expr)
        {
            return expr.CompileAndStore()();
        }

        /// <summary>
        /// Invoke expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Invoke<A0, T>(this Expression<Func<A0, T>> expr, A0 a0)
        {
            return expr.CompileAndStore()(a0);
        }

        /// <summary>
        /// Invoke expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Invoke<A0, A1, T>(this Expression<Func<A0, A1, T>> expr, A0 a0, A1 a1)
        {
            return expr.CompileAndStore()(a0, a1);
        }

        /// <summary>
        /// Invoke expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Invoke<A0, A1, A2, T>(this Expression<Func<A0, A1, A2, T>> expr, A0 a0, A1 a1, A2 a2)
        {
            return expr.CompileAndStore()(a0, a1, a2);
        }

        /// <summary>
        /// Invoke expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Invoke<A0, A1, A2, A3, T>(this Expression<Func<A0, A1, A2, A3, T>> expr, A0 a0, A1 a1, A2 a2, A3 a3)
        {
            return expr.CompileAndStore()(a0, a1, a2, a3);
        }

        /// <summary>
        /// Returns wrapper that automatically expands expressions in LINQ queries
        /// </summary>
        public static IQueryable<T> ToExpandable<T>(this IQueryable<T> q)
        {
            return new ExpandableQueryProvider<T>(q);
        }

        public static string NiceToString(this Expression expression)
        {
            if (expression == null)
                return null;

            return ExpressionToString.NiceToString(expression); 
        }
    }
}
