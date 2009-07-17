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
	/// Implementation of ExpressionVisiter that does the replacement
	/// </summary>
	public class ExpressionExpander : ExpressionVisitor
	{
        public static Expression ExpandUntyped(Expression expr)
        {
            return new ExpressionExpander().Visit(expr);
        }

		#region Initialization
        Dictionary<ParameterExpression, Expression> _replaceVars = new Dictionary<ParameterExpression, Expression>();
		#endregion

		#region Overrides
        protected override Expression VisitParameter(ParameterExpression p)
		{
			if ((_replaceVars != null) && (_replaceVars.ContainsKey(p)))
				return Visit(_replaceVars[p]);
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

		protected override Expression VisitMethodCall(MethodCallExpression m)
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
				
				for (int i = 0; i < lambda.Parameters.Count; i++)
				{
				   _replaceVars.Add(lambda.Parameters[i],  m.Arguments[i + 1]);
				}

                Expression result = Visit(lambda.Body);

                for (int i = 0; i < lambda.Parameters.Count; i++)
                {
                    _replaceVars.Remove(lambda.Parameters[i]);
                }

                return result; 
			}
			return base.VisitMethodCall(m);
		}
		#endregion
	}
}
