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

    //The name of the field for the expression that defines the content
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ExpressionFieldAttribute : Attribute
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public ExpressionFieldAttribute(string name)
        {
            this.Name =name;
        }
    }
     
	/// <summary>
    /// Implementation of SimpleExpressionVisitor that does the replacement
    /// * MethodExpanderAttribute
    /// * MemberXXXExpression static field
    /// * ExpressionExtensions.Expand method
    /// 
    /// It also simplifies and skip evaluating short circuited subexpresions
    /// Evaluates constant subexpressions 
	/// </summary>
	public class ExpressionCleaner: SimpleExpressionVisitor
	{
        Func<Expression, Expression> partialEval;

        bool shortCircuit;

        public static Expression Clean(Expression expr)
        {
            return Clean(expr, ExpressionEvaluator.PartialEval, true);
        }

        public static Expression Clean(Expression expr, Func<Expression, Expression> partialEval, bool shortCircuit)
        {
            ExpressionCleaner ee = new ExpressionCleaner()
            {
                partialEval = partialEval,
                shortCircuit = shortCircuit
            }; 
            var result = ee.Visit(expr);
            return partialEval(result);
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            if (iv.Expression is LambdaExpression)
                return Visit(ExpressionReplacer.Replace(iv));
            else
                return base.VisitInvocation(iv); //Just calling a delegate in the projector
        }

		protected override Expression VisitMethodCall(MethodCallExpression m)
		{
            MethodExpanderAttribute attribute = m.Method.SingleAttribute<MethodExpanderAttribute>();
			if (attribute != null)
			{
                IMethodExpander expander = Activator.CreateInstance(attribute.ExpanderType) as IMethodExpander;
				if (expander == null) 
                    throw new InvalidOperationException("Expansion failed, {0} does not implement IMethodExpander".Formato(attribute.ExpanderType.Name));

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

            LambdaExpression lambdaExpression = GetExpansion(m.Object.TryCC(c => c.Type), m.Method);
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
            if (pi == null)
                return base.VisitMemberAccess(m);

            LambdaExpression lambda = GetExpansion(m.Expression.TryCC(c => c.Type), pi);
            if (lambda != null)
            {
                if (m.Expression == null)
                    return Visit(lambda.Body);
                else
                    return Visit(Expression.Invoke(lambda, Visit(m.Expression)));
            }

            return base.VisitMemberAccess(m);
        }

        public static LambdaExpression GetExpansion(Type decType, MemberInfo mi)
        {
            ExpressionFieldAttribute efa = mi.SingleAttribute<ExpressionFieldAttribute>();

            string name = efa.TryCC(a => a.Name) ?? mi.Name + "Expression";
            Type type = efa.TryCC(a => a.Type) ?? decType ?? mi.DeclaringType;

            FieldInfo fi = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null)
                return fi.GetValue(null) as LambdaExpression;
            else
            {
                if (efa != null)
                    throw new InvalidOperationException("Expression field {0} not found on  {1}".Formato(name, type));

                if (type != mi.DeclaringType)
                {
                    fi = mi.DeclaringType.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fi != null)
                        return fi.GetValue(null) as LambdaExpression;
                }
            }

            return null;
        }

        #region Simplifier

        bool GetBool(Expression exp)
        {
            return (bool)((ConstantExpression)exp).Value;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (!shortCircuit)
                return base.VisitBinary(b); 

            if (b.NodeType == ExpressionType.Coalesce)
            {
                Expression left = partialEval(this.Visit(b.Left));

                if (left.NodeType == ExpressionType.Constant)
                {
                    var ce = (ConstantExpression)left;
                    if (ce.Value == null)
                        return Visit(b.Right);

                    if (ce.Type.IsNullable())
                        return Expression.Constant(ce.Value, ce.Type.UnNullify());
                    else
                        return ce;
                }

                Expression right = this.Visit(b.Right);
                Expression conversion = this.Visit(b.Conversion);

                return Expression.Coalesce(left, right, conversion as LambdaExpression);
            }

            if (b.Type != typeof(bool))
                return base.VisitBinary(b);

            if (b.NodeType == ExpressionType.And || b.NodeType == ExpressionType.AndAlso)
            {
                Expression left = partialEval(this.Visit(b.Left));
                if (left.NodeType == ExpressionType.Constant)
                    return GetBool(left) ? Visit(b.Right) : Expression.Constant(false);

                Expression right = partialEval(this.Visit(b.Right));
                if (right.NodeType == ExpressionType.Constant)
                    return GetBool(right) ? left : Expression.Constant(false);

                return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
            }
            else if (b.NodeType == ExpressionType.Or || b.NodeType == ExpressionType.OrElse)
            {
                Expression left = partialEval(this.Visit(b.Left));
                if (left.NodeType == ExpressionType.Constant)
                    return GetBool(left) ? Expression.Constant(true) : Visit(b.Right);

                Expression right = partialEval(this.Visit(b.Right));
                if (right.NodeType == ExpressionType.Constant)
                    return GetBool(right) ? Expression.Constant(true) : left;

                return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
            }

            if (b.Left.Type != typeof(bool))
                return base.VisitBinary(b);

            if (b.NodeType == ExpressionType.Equal)
            {
                Expression left = partialEval(this.Visit(b.Left));
                if (left.NodeType == ExpressionType.Constant)
                    return GetBool(left) ? Visit(b.Right) : Visit(Expression.Not(b.Right));

                Expression right = partialEval(this.Visit(b.Right));
                if (right.NodeType == ExpressionType.Constant)
                    return GetBool(right) ? left : Expression.Not(left);

                return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
            }
            else if (b.NodeType == ExpressionType.NotEqual)
            {
                Expression left = partialEval(this.Visit(b.Left));
                if (left.NodeType == ExpressionType.Constant)
                    return GetBool(left) ? Visit(Expression.Not(b.Right)) : Visit(b.Right);

                Expression right = partialEval(this.Visit(b.Right));
                if (right.NodeType == ExpressionType.Constant)
                    return GetBool(right) ? Expression.Not(left) : left;

                return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
            }

            return base.VisitBinary(b);
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            if (!shortCircuit)
                return base.VisitConditional(c); 

            Expression test = partialEval(this.Visit(c.Test));
            if (test.NodeType == ExpressionType.Constant)
            {
                if (GetBool(test))
                    return this.Visit(c.IfTrue);
                else
                    this.Visit(c.IfFalse);
            }

            Expression ifTrue = this.Visit(c.IfTrue);
            Expression ifFalse = this.Visit(c.IfFalse);
            if (test != c.Test || ifTrue != c.IfTrue || ifFalse != c.IfFalse)
            {
                return Expression.Condition(test, ifTrue, ifFalse);
            }
            return c;
        } 
        #endregion
	}
}
