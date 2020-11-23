using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Utilities
{
    public static class ExpressionExpanderSamples
	{
		/// <summary>
		/// Returns whether string contains any of the specified keywords as a substring.
		/// </summary>
		/// <param name="value">Value of the string</param>
		/// <param name="keywords">Array with keywords</param>
		/// <returns>True when value contains any of the specified keywords</returns>
		[MethodExpander(typeof(ExpandContainsAny))]
		public static bool ContainsAny(this string value, params string[] keywords)
		{
			return keywords.Any((s) => value.Contains(s, StringComparison.InvariantCultureIgnoreCase));
		}


		/// <summary>
		/// Returns whether string contains all of the specified keywords as a substring.
		/// </summary>
		/// <param name="value">Value of the string</param>
		/// <param name="keywords">Array with keywords</param>
		/// <returns>True when value contains all of the specified keywords</returns>
		[MethodExpander(typeof(ExpandContainsAll))]
		public static bool ContainsAll(this string value, params string[] keywords)
		{
			return keywords.All((s) => value.Contains(s, StringComparison.InvariantCultureIgnoreCase));
		}

        /// <summary>
        /// Returns whether the IQueryable contains no elements
        /// </summary>
        /// <typeparam name="TSource">IQueryable</typeparam>
        /// <param name="source">Value of the IQueryable</param>
        /// <returns>True iff the result of the IQueryable contains no elements</returns>
        [MethodExpander(typeof(ExpandNone))]
        public static bool None<TSource>(this IQueryable<TSource> source)
        {
            return !source.Any();
        }


        /// <summary>
        /// Returns whether the IQueryable contains no elements satisfying a predicate
        /// </summary>
        /// <typeparam name="TSource">IQueryable</typeparam>
        /// <param name="source">Value of the IQueryable</param>
        /// <param name="predicate">Predicate to satisfy</param>
        /// <returns>True iff the result of the IQueryable contains no elements satisfying a predicate</returns>
        [MethodExpander(typeof(ExpandNone))]
        public static bool None<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            return !source.Any(predicate);
        }

        /// <summary>
        /// Returns whether the IEnumerable contains no elements
        /// </summary>
        /// <typeparam name="TSource">IEnumerable</typeparam>
        /// <param name="source">Value of the IEnumerable</param>
        /// <returns>True iff the result of the IEnumerable contains no elements</returns>
        [MethodExpander(typeof(ExpandNone))]
        public static bool None<TSource>(this IEnumerable<TSource> source)
        {
            return !source.Any();
        }

        /// <summary>
        /// Returns whether the IEnumerable contains no elements satisfying a predicate
        /// </summary>
        /// <typeparam name="TSource">IEnumerable</typeparam>
        /// <param name="source">Value of the IEnumerable</param>
        /// <param name="predicate">Predicate to satisfy</param>
        /// <returns>True iff the result of the IEnumerable contains no elements satisfying a predicate</returns>
        [MethodExpander(typeof(ExpandNone))]
        public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            return !source.Any(predicate);
        }
    }

    class ExpandContainsAny : IMethodExpander
	{
        static readonly MethodInfo miContains = ReflectionTools.GetMethodInfo(() => "".Contains(""));

        public Expression Expand(Expression? instance, Expression[] parameters, MethodInfo mi)
        {
            var parts = (string[])ExpressionEvaluator.Eval(parameters[1])!;

            return parts.Select(p => (Expression)Expression.Call(parameters[0], miContains, Expression.Constant(p))).AggregateOr();
        }
	}

    class ExpandContainsAll : IMethodExpander
    {
        static readonly MethodInfo miContains = ReflectionTools.GetMethodInfo(() => "".Contains(""));

        public Expression Expand(Expression? instance, Expression[] parameters, MethodInfo mi)
        {
            var parts = (string[])ExpressionEvaluator.Eval(parameters[1])!;

            return parts.Select(p => (Expression)Expression.Call(parameters[0], miContains, Expression.Constant(p))).AggregateAnd();
        }
    }


    class ExpandNone : IMethodExpander 
    {
        static readonly MethodInfo miAnyEnumerable = ReflectionTools.GetMethodInfo(() => Enumerable.Any((IEnumerable<string>)null!)).GetGenericMethodDefinition();
        static readonly MethodInfo miAnyQueryable = ReflectionTools.GetMethodInfo(() =>Queryable.Any((IQueryable<string>)null!)).GetGenericMethodDefinition();
        static readonly MethodInfo miAnyEnumerableWithPredicate = ReflectionTools.GetMethodInfo(() =>Enumerable.Any((IEnumerable<string>)null!, null!)).GetGenericMethodDefinition();
        static readonly MethodInfo miAnyQueryableWithPredicate  = ReflectionTools.GetMethodInfo(() => Queryable.Any((IQueryable<string>)null!, null!)).GetGenericMethodDefinition();

        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            Type foo = mi.DeclaringType!;
            Type bar = typeof(Queryable);
            MethodInfo any = GetAny(mi).MakeGenericMethod(mi.GetGenericArguments());
            return Expression.Not(Expression.Call(any, arguments));
        }

        private MethodInfo GetAny(MethodInfo mi)
        {
            var parameters = mi.GetParameters();
            bool query = parameters[0].ParameterType.IsInstantiationOf(typeof(IQueryable<>));
            if (parameters.Length == 1)
                return query ? miAnyQueryable : miAnyEnumerable;
            else
                return query ? miAnyQueryableWithPredicate : miAnyEnumerableWithPredicate;
        }
    }

}
