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
    /// Implementation of SimpleExpressionVisitor that does the replacement
    /// * MethodExpanderAttribute
    /// * MemberXXXExpression static field
    /// * ExpressionExtensions.Expand method
    /// 
    /// It also simplifies and skip evaluating short circuited subexpresions
    /// Evaluates constant subexpressions 
	/// </summary>
    public class ExpressionCleaner : ExpressionVisitor
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
            MethodCallExpression expr = (MethodCallExpression)base.VisitMethodCall(m);

            Expression binded =  BindMethodExpression(expr, false);

            if (binded != null)
                return Visit(binded);

            return expr;
		}

        public static Expression BindMethodExpression(MethodCallExpression m, bool allowPolymorphics)
        {
            if (m.Method.DeclaringType == typeof(ExpressionExtensions) && m.Method.Name == "Evaluate")
            {
                LambdaExpression lambda = (LambdaExpression)(ExpressionEvaluator.Eval(m.Arguments[0]));

                return Expression.Invoke(lambda, m.Arguments.Skip(1).ToArray());
            }

            if (m.Method.HasAttributeInherit<PolymorphicExpansionAttribute>() && !allowPolymorphics)
                return null;

            MethodExpanderAttribute attribute = m.Method.GetCustomAttribute<MethodExpanderAttribute>();
            if (attribute != null)
            {
                if (attribute.ExpanderType.IsGenericTypeDefinition)
                {
                    if (!typeof(GenericMethodExpander).IsAssignableFrom(attribute.ExpanderType))
                        throw new InvalidOperationException("Expansion failed, '{0}' does not implement IMethodExpander or GenericMethodExpander".FormatWith(attribute.ExpanderType.TypeName()));
                    
                    Expression[] args = m.Object == null ? m.Arguments.ToArray() : m.Arguments.PreAnd(m.Object).ToArray();

                    var type = attribute.ExpanderType.MakeGenericType(m.Method.GetGenericArguments());
                    GenericMethodExpander expander = Activator.CreateInstance(type) as GenericMethodExpander;
                    return Expression.Invoke(expander.GenericLambdaExpression, args);
                }
                else
                {
                    if(!typeof(IMethodExpander).IsAssignableFrom(attribute.ExpanderType))
                        throw new InvalidOperationException("Expansion failed, '{0}' does not implement IMethodExpander or GenericMethodExpander".FormatWith(attribute.ExpanderType.TypeName()));

                    IMethodExpander expander = (IMethodExpander)Activator.CreateInstance(attribute.ExpanderType);

                    Expression exp = expander.Expand(
                        m.Object,
                        m.Arguments.ToArray(),
                        m.Method);

                    return exp;
                }
            }

            LambdaExpression lambdaExpression = GetFieldExpansion(m.Object?.Type, m.Method);
            if (lambdaExpression != null)
            {
                Expression[] args = m.Object == null ? m.Arguments.ToArray() : m.Arguments.PreAnd(m.Object).ToArray();

                return Expression.Invoke(lambdaExpression, args);
            }

            return null;

        }

        protected override Expression VisitMember(MemberExpression m)
        {
            MemberExpression exp = (MemberExpression)base.VisitMember(m);

            Expression binded = BindMemberExpression(exp, false);

            if (binded != null)
                return Visit(binded);

            return exp;
        }
        
        public static Expression BindMemberExpression(MemberExpression m, bool allowPolymorphics)
        {
            PropertyInfo pi = m.Member as PropertyInfo;
            if (pi == null)
                return null;

            if (pi.HasAttributeInherit<PolymorphicExpansionAttribute>() && !allowPolymorphics)
                return null;

            LambdaExpression lambda = GetFieldExpansion(m.Expression?.Type, pi);
            if (lambda == null)
                return null;

            if (m.Expression == null)
                return lambda.Body;
            else
                return Expression.Invoke(lambda, m.Expression);
        }

        public static bool HasExpansions(Type type, MemberInfo mi)
        {
            return GetFieldExpansion(type, mi) != null || mi is MethodInfo && mi.HasAttribute<MethodExpanderAttribute>();
        }

        public static LambdaExpression GetFieldExpansion(Type decType, MemberInfo mi)
        {
            if (decType == null || decType == mi.DeclaringType || IsStatic(mi))
                return GetExpansion(mi);
            else
            {
                for (MemberInfo m = GetMember(decType, mi); m != null; m = BaseMember(m))
                {
                    var result = GetExpansion(m);
                    if (result != null)
                        return result;
                }

                return null; 
            }
        }

        static bool IsStatic(MemberInfo mi)
        {
            if (mi is MethodInfo)
                return ((MethodInfo)mi).IsStatic;

            if (mi is PropertyInfo)
                return (((PropertyInfo)mi).GetGetMethod() ?? ((PropertyInfo)mi).GetSetMethod()).IsStatic;

            return false;
        }

        static LambdaExpression GetExpansion(MemberInfo mi)
        {
            ExpressionFieldAttribute efa = mi.GetCustomAttribute<ExpressionFieldAttribute>();
            if (efa == null)
                return null;

            if (efa.Name == "auto")
                throw new InvalidOperationException($"The {nameof(ExpressionFieldAttribute)} for {mi.DeclaringType.TypeName()}.{mi.MemberName()} has the default value 'auto'.\r\nMaybe Signum.MSBuildTask is not running in assemby {mi.DeclaringType.Assembly.GetName().Name}?");

            Type type = mi.DeclaringType;
            FieldInfo fi = type.GetField(efa.Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null)
                throw new InvalidOperationException("Expression field '{0}' not found on '{1}'".FormatWith(efa.Name, type.TypeName()));

            var obj = fi.GetValue(null);

            if (obj == null)
                throw new InvalidOperationException("Expression field '{0}' is null".FormatWith(efa.Name));

            var result = obj as LambdaExpression;

            if (result == null)
                throw new InvalidOperationException("Expression field '{0}' does not contain a lambda expression".FormatWith(efa.Name, type.TypeName()));

            return result;
        }

        static BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static MemberInfo GetMember(Type decType, MemberInfo mi)
        {
            if (mi is MethodInfo)
            {
                Type[] types = ((MethodInfo)mi).GetParameters().Select(a => a.ParameterType).ToArray();
                var result = decType.GetMethod(mi.Name, flags, null, types, null);
                if (result != null)
                    return result;

                if (mi.DeclaringType.IsInterface)
                    return decType.GetMethod(mi.DeclaringType.FullName + "." + mi.Name, flags, null, types, null);

                return null;
            }

            if (mi is PropertyInfo)
            {
                Type[] types = ((PropertyInfo)mi).GetIndexParameters().Select(a => a.ParameterType).ToArray();

                var result = decType.GetProperty(mi.Name, flags, null, ((PropertyInfo)mi).PropertyType, types, null) ;
                if (result != null)
                    return result; 

                if(mi.DeclaringType.IsInterface)
                    return decType.GetProperty(mi.DeclaringType.FullName + "." + mi.Name, flags, null, ((PropertyInfo)mi).PropertyType, types, null);

                return null;
            }

            throw new InvalidOperationException("Invalid Member type"); 
        }

        static MemberInfo BaseMember(MemberInfo mi)
        {
            MemberInfo result;
            if (mi is MethodInfo)
                result = ((MethodInfo)mi).GetBaseDefinition();

            else if (mi is PropertyInfo)
                result = ((PropertyInfo)mi).GetBaseDefinition();
            else
                throw new InvalidOperationException("Invalid Member type");

            if (result == mi)
                return null;

            return result; 
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
                    return this.Visit(c.IfFalse);
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
