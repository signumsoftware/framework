using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Reflection;

namespace Signum.Windows.UIAutomation
{
    public static class ConditionBuilder
    {
        public static Condition ToCondition(Expression<Func<AutomationElement, bool>> condition)
        {
            return ToCondition(condition.Parameters.Single(), condition.Body);
        }

        static Condition ToCondition(ParameterExpression p, Expression body)
        {
            if (body.NodeType == ExpressionType.Not)
                return new NotCondition(ToCondition(p, ((UnaryExpression)body).Operand));

            BinaryExpression be = body as BinaryExpression;
            if (be != null)
            {
                if (be.NodeType == ExpressionType.And || be.NodeType == ExpressionType.AndAlso)
                    return new AndCondition(ToCondition(p, be.Left), ToCondition(p, be.Right));

                if (be.NodeType == ExpressionType.Or || be.NodeType == ExpressionType.OrElse)
                    return new OrCondition(ToCondition(p, be.Left), ToCondition(p, be.Right));

                if (be.NodeType == ExpressionType.Equal || be.NodeType == ExpressionType.NotEqual)
                {
                    var cond = ToPropertyCondition(p, be.Left, be.Right);

                    if (cond != null)
                    {
                        if (be.NodeType == ExpressionType.Equal)
                            return cond;

                        if (cond.Value is bool)
                            new PropertyCondition(cond.Property, !(bool)cond.Value);

                        return new NotCondition(cond);
                    }
                }
            }

            var constant = ExpressionEvaluator.PartialEval(body);
            if (constant is ConstantExpression && ((ConstantExpression)constant).Value is bool)
                return ((bool)((ConstantExpression)constant).Value) ? PropertyCondition.TrueCondition : PropertyCondition.FalseCondition;

            throw new InvalidOperationException("The expression can not be translated to a UIAutomation Condition {0}".Formato(ExpressionEvaluator.PartialEval(body).NiceToString()));
        }

        static PropertyCondition ToPropertyCondition(ParameterExpression p, Expression left, Expression right)
        {
            AutomationProperty prop = ToAutomationProperty(p, left);
            if (prop != null)
                return new PropertyCondition(prop, ExpressionEvaluator.Eval(right));

            Type pattern = GetPattern(p, left);
            if (pattern != null && IsNull(right))
                return new PropertyCondition(GetCachedProperty(pattern, "Is{0}AvailableProperty".Formato(pattern.Name)), false);

            prop = ToAutomationProperty(p, right);
            if (prop != null)
                return new PropertyCondition(prop, ExpressionEvaluator.Eval(left));


            pattern = GetPattern(p, right);
            if (pattern != null && IsNull(left))
                return new PropertyCondition(GetCachedProperty(pattern, "Is{0}AvailableProperty".Formato(pattern.Name)), false);

            return null;
        }

        static bool IsNull(Expression right)
        {
            return right.NodeType == ExpressionType.Constant && ((ConstantExpression)right).Value == null;
        }


        static AutomationProperty ToAutomationProperty(ParameterExpression p, Expression node)
        {
            if (node.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression me = (MemberExpression)node;

                MemberExpression cme = me.Expression as MemberExpression;

                if (cme != null && (cme.Member.Name == "Current" || cme.Member.Name == "Cached"))
                {
                    if (cme.Expression == p)
                        return GetCachedProperty(typeof(AutomationElement), me.Member.Name);

                    Type pattern = GetPattern(p, cme.Expression);

                    if (pattern != null)
                        return GetCachedProperty(pattern, me.Member.Name);
                }
            }

            return null;
        }

        static Type GetPattern(ParameterExpression p, Expression node)
        {
            if (node.NodeType == ExpressionType.Call)
            {
                MethodCallExpression mce = (MethodCallExpression)node;

                if (mce.GetArgument("ae") == p && (mce.Method.Name == "Pattern" || mce.Method.Name == "TryPattern"))
                    return mce.Method.GetGenericArguments()[0];
            }

            return null;
        }


        static readonly Dictionary<Type, Dictionary<string, AutomationProperty>> properties = new Dictionary<Type, Dictionary<string, AutomationProperty>>();
        static AutomationProperty GetCachedProperty(Type type, string propertyName)
        {
            var dic = properties.GetOrCreate(type, () =>
                  type.GetFields(BindingFlags.Static | BindingFlags.Public)
                  .Where(p => p.Name.EndsWith("Property"))
                  .ToDictionary(p => p.Name.RemoveEnd("Property".Length), p => (AutomationProperty)p.GetValue(null)));

            var result = dic.TryGetC(propertyName);

            if (result == null)
                throw new InvalidOperationException("Property {0} is not accessible on UIAutomation queries".Formato(propertyName));

            return result;
        }
    }
}
