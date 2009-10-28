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
        Expression Expand(Expression instance, Expression[] arguments);
	}

	/// <summary>
    /// Attribute to define the class that should be used to convert calls to methods
    /// in LINQ expression trees
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class MethodExpanderAttribute : Attribute
	{
		private Type expanderType;
		public Type ExpanderType
		{
			get { return expanderType; }
		}

        /// <param name="type">A class that implements IMethodExpander</param>
		public MethodExpanderAttribute(Type type)
		{
			expanderType = type;
		}
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

        Dictionary<ParameterExpression, Expression> replacements = new Dictionary<ParameterExpression, Expression>();

        protected override Expression VisitParameter(ParameterExpression p)
		{
			if ((replacements != null) && (replacements.ContainsKey(p)))
				return Visit(replacements[p]);
			else
				return base.VisitParameter(p);
		}

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            LambdaExpression lambda = iv.Expression as LambdaExpression;
            if (lambda != null)
            {
                for (int i = 0, n = lambda.Parameters.Count; i < n; i++)
                    replacements[lambda.Parameters[i]] = iv.Arguments[i];

                Expression result = this.Visit(lambda.Body);

                for (int i = 0, n = lambda.Parameters.Count; i < n; i++)
                    replacements.Remove(lambda.Parameters[i]);

                return result; 
            }
            return base.VisitInvocation(iv);
        }


		protected override Expression VisitMethodCall(MethodCallExpression m)
		{
            MethodExpanderAttribute attribute = m.Method.SingleAttribute<MethodExpanderAttribute>();
			if (attribute != null)
			{
                IMethodExpander exp = Activator.CreateInstance(attribute.ExpanderType) as IMethodExpander;
				if (exp == null) 
                    throw new InvalidOperationException("Expansion failed! '{0}' does not implement IMethodExpander".Formato(attribute.ExpanderType.Name));
                return Visit(exp.Expand(Visit(m.Object), m.Arguments.Select(p => Visit(p)).ToArray()));
			}

			if (m.Method.DeclaringType == typeof(ExpressionExtensions))
			{
				LambdaExpression lambda = (LambdaExpression)(ExpressionEvaluator.Eval(m.Arguments[0]));

                return Visit(Expression.Invoke(lambda, m.Arguments.Skip(1).Select(a=>Visit(a)).ToArray()));
			}

            LambdaExpression lambdaExpression = ExtractAndClean(m.Method);
            if(lambdaExpression != null)
            {
                Expression[] args =  m.Object == null ? m.Arguments.ToArray() : m.Arguments.PreAnd(m.Object).ToArray();

                return Visit(Expression.Invoke(lambdaExpression, args.Select(e => Visit(e)).ToArray()));
            }

			return base.VisitMethodCall(m);
		}

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            PropertyInfo pi = m.Member as PropertyInfo;
            if(pi == null)
                 return base.VisitMemberAccess(m);

            LambdaExpression lambda = ExtractAndClean(pi); 
            if(lambda ==null)
                return base.VisitMemberAccess(m);

            if(m.Expression == null)
                return lambda.Body;
            else 
                return Visit(Expression.Invoke(lambda, Visit(m.Expression)));
        }

        static LambdaExpression ExtractAndClean(MemberInfo mi)
        {
            FieldInfo fi = mi.DeclaringType.GetField(mi.Name + "Expression", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null)
                return null;

            return fi.GetValue(null) as LambdaExpression;
        }
	}
}
