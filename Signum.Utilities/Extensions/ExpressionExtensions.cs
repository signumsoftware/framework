using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using System.Collections.Concurrent;

namespace Signum.Utilities
{
    /// <summary>
    /// Contains extension methods for Expression class. These methods
    /// can be used to 'call' expression tree and can be translated to IQueryable
    /// </summary>
    public static class ExpressionExtensions
    {
        static ConcurrentDictionary<LambdaExpression, Delegate> cache = new ConcurrentDictionary<LambdaExpression, Delegate>();

        static Dictionary<LambdaExpression, LambdaExpression> registeredExpressions = new Dictionary<LambdaExpression, LambdaExpression>(ExpressionComparer.GetComparer<LambdaExpression>(checkParameterNames: true)); 

        public static T CompileAndStore<T>(this Expression<T> expression)
        {
            return (T)(object)cache.GetOrAdd(expression, exp =>
            {
                using (HeavyProfiler.Log("CompiledAndStore", () => exp.ToString()))
                lock(registeredExpressions)
                {
                    var already = registeredExpressions.TryGetC(exp);
                    if (already != null && already != exp)
                        throw new InvalidOperationException(DuplicateMessage(exp, already));

                    var res = (Delegate)(object)exp.Compile();
                    registeredExpressions[exp] = exp;
                    return res;
                }
            });
        }

        private static string DuplicateMessage(LambdaExpression exp, LambdaExpression already)
        {
            return @"Can not cache the compiled version of expression: 
{0}
Because a similar expression has already been cached: 
{1}
This limitation tries to avoid running out of memory caching freshly generated expressions. 
Use this method only with constant expressions stored in static fields.".FormatWith(exp.ToString(), already.ToString());
        }

        /// <summary>
        /// Evaluate expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Evaluate<T>(this Expression<Func<T>> expr)
        {
            return expr.CompileAndStore()();
        }

        /// <summary>
        /// Evaluate expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Evaluate<A0, T>(this Expression<Func<A0, T>> expr, A0 a0)
        {
            return expr.CompileAndStore()(a0);
        }

        /// <summary>
        /// Evaluate expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Evaluate<A0, A1, T>(this Expression<Func<A0, A1, T>> expr, A0 a0, A1 a1)
        {
            return expr.CompileAndStore()(a0, a1);
        }

        /// <summary>
        /// Evaluate expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Evaluate<A0, A1, A2, T>(this Expression<Func<A0, A1, A2, T>> expr, A0 a0, A1 a1, A2 a2)
        {
            return expr.CompileAndStore()(a0, a1, a2);
        }

        /// <summary>
        /// Evaluate expression (compile & invoke). If you want to be able to expand
        /// call to expression you have to use this method for invocation.
        /// </summary>
        public static T Evaluate<A0, A1, A2, A3, T>(this Expression<Func<A0, A1, A2, A3, T>> expr, A0 a0, A1 a1, A2 a2, A3 a3)
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

    }
}
