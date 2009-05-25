using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;


namespace Signum.Utilities.ExpressionTrees
{
	public static class GeneralUtils
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
			return keywords.Any((s) => value.Contains(s));
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
			return keywords.All((s) => value.Contains(s));
		}
	}

	public static class Linq
	{
        /// <summary>
        /// Utility function for building expression trees for lambda functions
        /// that return C# anonymous type as a result (because you can't declare 
        /// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
        /// </summary>
        public static Expression<Func<R>> Expr<R>(Expression<Func<R>> f)
        {
            return f;
        }

		/// <summary>
		/// Utility function for building expression trees for lambda functions
		/// that return C# anonymous type as a result (because you can't declare 
		/// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
		/// </summary>
		public static Expression<Func<T, R>> Expr<T, R>(Expression<Func<T, R>> f)
		{
			return f;
		}

		/// <summary>
		/// Utility function for building expression trees for lambda functions
		/// that return C# anonymous type as a result (because you can't declare 
		/// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
		/// </summary>
		public static Expression<Func<T0, T1, R>> Expr<T0, T1, R>(Expression<Func<T0, T1, R>> f)
		{
			return f;
		}

		/// <summary>
		/// Utility function for building expression trees for lambda functions
		/// that return C# anonymous type as a result (because you can't declare 
		/// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
		/// </summary>
		public static Expression<Func<T0, T1, T2, R>> Expr<T0, T1, T2, R>(Expression<Func<T0, T1, T2, R>> f)
		{
			return f;
		}

		/// <summary>
		/// Utility function for building expression trees for lambda functions
		/// that return C# anonymous type as a result (because you can't declare 
		/// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
		/// </summary>
		public static Expression<Func<T0, T1, T2, T3, R>> Expr<T0, T1, T2, T3, R>(Expression<Func<T0, T1, T2, T3, R>> f)
		{
			return f;
		}

		/// <summary>
		/// Utility function for building delegates for lambda functions
		/// that return C# anonymous type as a result (because you can't declare 
		/// it using delegates&lt;Func&lt;...&gt;&gt; syntax)
		/// </summary>
		public static Func<T, R> Func<T, R>(Func<T, R> f)
		{
			return f;
		}

		/// <summary>
		/// Utility function for building delegates for lambda functions
		/// that return C# anonymous type as a result (because you can't declare 
		/// it using delegates&lt;Func&lt;...&gt;&gt; syntax)
		/// </summary>
		public static Func<T0, T1, R> Func<T0, T1, R>(Func<T0, T1, R> f)
		{
			return f;
		}

		/// <summary>
		/// Utility function for building delegates for lambda functions
		/// that return C# anonymous type as a result (because you can't declare 
		/// it using delegates&lt;Func&lt;...&gt;&gt; syntax)
		/// </summary>
		public static Func<T0, T1, T2, R> Func<T0, T1, T2, R>(Func<T0, T1, T2, R> f)
		{
			return f;
		}

		/// <summary>
		/// Utility function for building delegates for lambda functions
		/// that return C# anonymous type as a result (because you can't declare 
		/// it using delegates&lt;Func&lt;...&gt;&gt; syntax)
		/// </summary>
		public static Func<T0, T1, T2, T3, R> Func<T0, T1, T2, T3, R>(Func<T0, T1, T2, T3, R> f)
		{
			return f;
		}
	}

	#region MethodExpanders

	abstract class StringArrayExpanderBase : IMethodExpander
	{
		protected MethodInfo contMeth;
		protected Expression selfRef;
		protected abstract string MethodName { get; }
		protected abstract Func<Expression, string, Expression> Agg { get; }

		public Expression Expand(Expression selfRef, IEnumerable<Expression> parameters)
		{
			this.selfRef = selfRef;
			string[] vals;
			try
			{
				vals = (string[])ExpressionExpander.Evaluate(parameters.First());
				if (vals.Length == 0) throw new Exception();
			}
			catch
			{
				throw new ArgumentException(string.Format("First argument for the '{0}' method must be non empty string array!", MethodName));
			}

			// Init parameters
			contMeth = typeof(string).GetMethod("Contains");

			// Combine using And when method is ContainsAll or using Or when method is ConainsAny
			var init = Expression.Call(selfRef, contMeth, Expression.Constant(vals[0]));
            return vals.Skip(1).Aggregate(init, Agg);
		}
	}

	class ExpandContainsAny : StringArrayExpanderBase
	{
		protected override string MethodName
		{
			get { return "ContainsAny"; }
		}

		protected override Func<Expression, string, Expression> Agg
		{
			get 
			{ 
				return ( expr, str) => Expression.Or(expr,
					Expression.Call(selfRef, contMeth, Expression.Constant(str))); 
			}
		}
	}

	class ExpandContainsAll : StringArrayExpanderBase
	{
		protected override string MethodName
		{
			get { return "ContainsAll"; }
		}

		protected override Func<Expression, string, Expression> Agg
		{
			get
			{
				return ( expr, str) => Expression.And(expr,
					Expression.Call(selfRef, contMeth, Expression.Constant(str))); 
			}
		}
	}

	#endregion
}
