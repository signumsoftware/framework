using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Properties;

namespace Signum.Utilities.ExpressionTrees
{
	/// <summary>
	/// Interface for classes that can be used to convert calls to methods
	/// in LINQ expression trees.
	/// </summary>
	public interface IMethodExpander
	{
        Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments);
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
    /// Implementation of ExpressionVisitor that does the replacement
    /// * MethodExpanderAttribute
    /// * MemberXXXExpression static field
    /// * ExpressionExtensions.Expand method
	/// </summary>
	public class ExpressionExpander : ExpressionVisitor
	{
        Func<Expression, Expression> onExpand; 

        public static Expression Expand(Expression expr, Func<Expression, Expression> onExpand)
        {
            return new ExpressionExpander() { onExpand = onExpand }.Visit(expr);
        }

        public static Expression Expand(Expression expr)
        {
            ExpressionExpander ee = new ExpressionExpander();
            ee.onExpand = ee.Visit;
            return ee.Visit(expr);
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            return onExpand(ExpressionReplacer.Replace(iv)); 
        }

		protected override Expression VisitMethodCall(MethodCallExpression m)
		{
            MethodExpanderAttribute attribute = m.Method.SingleAttribute<MethodExpanderAttribute>();
			if (attribute != null)
			{
                IMethodExpander expander = Activator.CreateInstance(attribute.ExpanderType) as IMethodExpander;
				if (expander == null) 
                    throw new InvalidOperationException(Resources.ExpansionFailed0DoesNotImplementIMethodExpander.Formato(attribute.ExpanderType.Name));

                Expression exp = expander.Expand(
                    Visit(m.Object), 
                    m.Arguments.Select(p => Visit(p)).ToArray(), 
                    m.Method.IsGenericMethod ? m.Method.GetGenericArguments() : null);

                return Visit(exp);
			}

            if (m.Method.DeclaringType == typeof(ExpressionExtensions) && m.Method.Name == "Invoke")
            {
                LambdaExpression lambda = (LambdaExpression)(ExpressionEvaluator.Eval(m.Arguments[0]));

                return Visit(Expression.Invoke(lambda, m.Arguments.Skip(1).Select(a => Visit(a)).ToArray()));
            }

            LambdaExpression lambdaExpression = ExtractAndClean(m.Object.TryCC(c => c.Type), m.Method);
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

            LambdaExpression lambda = ExtractAndClean(m.Expression.TryCC(c => c.Type), pi);
            if(lambda ==null)
                return base.VisitMemberAccess(m);

            if(m.Expression == null)
                return Visit(lambda.Body);
            else
                return Visit(Expression.Invoke(lambda, Visit(m.Expression)));
        }

        static LambdaExpression ExtractAndClean(Type decType, MemberInfo mi)
        {
            FieldInfo fi;
            if(decType != null)
            {
                fi = decType.GetField(mi.Name + "Expression", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null)
                    return fi.GetValue(null) as LambdaExpression;

                if (decType == mi.DeclaringType)
                    return null; 
            }

            fi = mi.DeclaringType.GetField(mi.Name + "Expression", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null)
                return fi.GetValue(null) as LambdaExpression;

            return null;
        }
	}
}
