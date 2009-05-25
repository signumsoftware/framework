using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Utilities.ExpressionTrees
{
	/// <summary>
	/// Interface for classes that can be used to convert calls to methods
	/// in LINQ expression trees.
	/// </summary>
	public interface IMethodExpander
	{
		/// <summary>
		/// Converts calls to method. This method is called when converting
		/// expression tree before it is converted to SQL/or other target.
		/// </summary>
		/// <param name="selfRef">Represents reference to object (on which the method is invoked)</param>
		/// <param name="parameters">Represents other parameters</param>
		/// <returns>Method should return converted string</returns>
		Expression Expand(Expression selfRef, IEnumerable<Expression> parameters);
	}


	/// <summary>
	/// Using this attribute you can define class that should be used for expanding 
	/// calls to the method befor converting Expression tree to SQL (or other target).
	/// This attribute attaches implementation of <see cref="IMethodExpander"/> interface 
	/// to method.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class MethodExpanderAttribute : Attribute
	{
		#region Members

		private Type _type;

		/// <summary>
		/// Type that implements IMethodExpander interface
		/// </summary>
		public Type ExpanderType
		{
			get { return _type; }
			set { _type = value; }
		}

		public MethodExpanderAttribute(Type type)
		{
			_type = type;
		}

		#endregion
	}

	/// <summary>
	/// Contains extension methods for Expression class. These methods
	/// can be used to 'call' expression tree and can be translated to IQueryable
	/// </summary>
	public static class ExpressionExtensions
	{
		#region Extension methods

		/// <summary>
		/// Invoke expression (compile & invoke). If you want to be able to expand
		/// call to expression you have to use this method for invocation.
		/// </summary>
		public static T Expand<A0, T>(this Expression<Func<A0, T>> expr, A0 a0)
		{
			return expr.Compile().Invoke(a0);
		}

		/// <summary>
		/// Takes expr and replaces all calls to Expand (extension) method by it's implementation 
		/// (modifies expression tree)
		/// </summary>
		public static Expression<Func<A0, T>> Expand<A0, T>(this Expression<Func<A0, T>> expr)
		{
			return (Expression<Func<A0, T>>)new ExpressionExpander().Visit(expr);
		}

		/// <summary>
		/// Takes expr and replaces all calls to Expand (extension) method by it's implementation 
		/// (modifies expression tree)
		/// </summary>
		public static Expression<Func<A0, A1, T>> Expand<A0, A1, T>(this Expression<Func<A0, A1, T>> expr)
		{
			return (Expression<Func<A0, A1, T>>)new ExpressionExpander().Visit(expr);
		}

		/// <summary>
		/// Takes expr and replaces all calls to Expand (extension) method by it's implementation 
		/// (modifies expression tree)
		/// </summary>
		public static Expression ExpandUntyped(this Expression expr)
		{
			return new ExpressionExpander().Visit(expr);
		}

		#endregion
	}


	/// <summary>
	/// Implementation of ExpressionVisiter that does the replacement
	/// </summary>
	internal class ExpressionExpander : ExpressionVisitor
	{
		#region Initialization

		Dictionary<ParameterExpression,Expression> _replaceVars;

		internal ExpressionExpander()
		{
			_replaceVars = null;
		}

		private ExpressionExpander(Dictionary<ParameterExpression,Expression> replaceVars)
		{
			_replaceVars = replaceVars;
		}

		#endregion

		#region Overrides
		internal override Expression VisitParameter(ParameterExpression p)
		{
			if ((_replaceVars != null) && (_replaceVars.ContainsKey(p)))
				return _replaceVars[p];
			else
				return base.VisitParameter(p);
		}

		internal static object Evaluate(Expression e)
		{
			if (e is ConstantExpression) return ((ConstantExpression)e).Value;
			if (e is MemberExpression)
			{
				var me = (MemberExpression)e;
				var obj = Evaluate(me.Expression);
				var prop = (me.Member as PropertyInfo);
				if (prop != null) 
                    return prop.GetValue(obj, new object[0]);
				var fld = (me.Member as FieldInfo);
				if (fld != null) 
                    return fld.GetValue(obj);
			}
			if (e is NewArrayExpression)
			{
				var ar = (NewArrayExpression)e;
				var args = ar.Expressions.Select((expr) => Evaluate(expr)).ToArray();
				var res = Array.CreateInstance(ar.Type.GetElementType(), args.Length);
				for (int i = 0; i < res.Length; i++)
					res.SetValue(args[i], i);
				return res;
			}
			throw new Exception("Cannot evaluate expression!");
		}

		internal override Expression VisitMethodCall(MethodCallExpression m)
		{
			// Expand expression tree 'calls'
			object[] attrs = m.Method.GetCustomAttributes(typeof(MethodExpanderAttribute), false);
			if (attrs.Length > 0)
			{
				MethodExpanderAttribute attr = (MethodExpanderAttribute)attrs[0];
				IMethodExpander exp = Activator.CreateInstance(attr.ExpanderType) as IMethodExpander;
				if (exp == null) throw new InvalidOperationException(string.Format(
					"LINQ method mapping expansion failed! Type '{0}' does not implement IMethodExpander interface.",
					attr.ExpanderType.Name));
				return exp.Expand(Visit(m.Arguments[0]), m.Arguments.Skip(1).Select(p => Visit(p)));
			}

			if (m.Method.DeclaringType == typeof(ExpressionExtensions))
			{
				LambdaExpression lambda = (LambdaExpression)(Evaluate(m.Arguments[0]));
				
				Dictionary<ParameterExpression,Expression> replaceVars
					= new Dictionary<ParameterExpression,Expression>();
				for (int i = 0; i < lambda.Parameters.Count; i++)
				{
					Expression rep = m.Arguments[i + 1];
					if ((_replaceVars != null) && (rep is ParameterExpression) && (_replaceVars.ContainsKey((ParameterExpression)rep)))
						replaceVars.Add(lambda.Parameters[i], _replaceVars[(ParameterExpression)rep]);
					else
						replaceVars.Add(lambda.Parameters[i], rep);
				}
				if (_replaceVars != null)
				{
					foreach (KeyValuePair<ParameterExpression, Expression> pair in _replaceVars)
						replaceVars.Add(pair.Key, pair.Value);
				}
				return new ExpressionExpander(replaceVars).Visit(lambda.Body);
			}
			return base.VisitMethodCall(m);
		}
		#endregion
	}
}