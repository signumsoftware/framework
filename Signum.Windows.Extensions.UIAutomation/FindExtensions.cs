using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using System.Threading;

namespace Signum.Windows.UIAutomation
{
    public static class FindExtensions
    {
        public static AutomationElement TryDescendantById(this AutomationElement parent, string automationId)
        {
            return parent.TryElementById(TreeScope.Descendants, automationId);
        }

        public static AutomationElement TryChildById(this AutomationElement parent, string automationId)
        {
            return parent.TryElementById(TreeScope.Children, automationId);
        }

        public static AutomationElement TryElementById(this AutomationElement parent, TreeScope scope, string automationId)
        {
            return parent.FindFirst(scope, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
        }


        public static AutomationElement DescendantById(this AutomationElement parent, string automationId)
        {
            return parent.ElementById(TreeScope.Descendants, automationId);
        }

        public static AutomationElement ChildById(this AutomationElement parent, string automationId)
        {
            return parent.ElementById(TreeScope.Children, automationId);
        }

        public static AutomationElement ElementById(this AutomationElement parent, TreeScope scope, string automationId)
        {
            var result = parent.FindFirst(scope, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));

            if (result == null)
                throw new KeyNotFoundException("No AutomationElement found with AutomationID '{0}'".Formato(automationId));

            return result;
        }




        public static AutomationElement TryDescendantByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.TryElementByCondition(TreeScope.Descendants, condition);
        }

        public static AutomationElement TryChildByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.TryElementByCondition(TreeScope.Children, condition);
        }

        public static AutomationElement TryElementByCondition(this AutomationElement parent, TreeScope scope, Condition condition)
        {
            return parent.FindFirst(scope, new PropertyCondition(AutomationElement.AutomationIdProperty, condition));
        }


        public static AutomationElement DescendantByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.ElementByCondition(TreeScope.Descendants, condition);
        }

        public static AutomationElement ChildByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.ElementByCondition(TreeScope.Children, condition);
        }

        public static AutomationElement ElementByCondition(this AutomationElement parent, TreeScope scope, Condition condition)
        {
            var result = parent.FindFirst(scope, condition);

            if (result == null)
                throw new KeyNotFoundException("No AutomationElement found: {0}".Formato(condition.NiceToString()));

            return result;
        }

   

        public static AutomationElement TryDescendant(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.TryElement(TreeScope.Descendants, condition);
        }

        public static AutomationElement TryChild(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.TryElement(TreeScope.Children, condition);
        }

        public static AutomationElement TryElement(this AutomationElement parent, TreeScope scope, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.FindFirst(scope, ToCondition(condition));
        }


        public static AutomationElement Descendant(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.Element(TreeScope.Descendants, condition);
        }

        public static AutomationElement Child(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.Element(TreeScope.Children, condition);
        }

        public static AutomationElement Element(this AutomationElement parent, TreeScope scope, Expression<Func<AutomationElement, bool>> condition)
        {
            var c = ToCondition(condition); 

            var result = parent.FindFirst(scope, c);

            if (result == null)
                throw new KeyNotFoundException("No AutomationElement found with condition {0}".Formato(ExpressionEvaluator.PartialEval(condition).NiceToString()));

            return result;
        }

        public static List<AutomationElement> DescendantsAll(this AutomationElement parent)
        {
            return parent.ElementsByCondition(TreeScope.Descendants, PropertyCondition.TrueCondition);
        }

        public static List<AutomationElement> ChildrenAll(this AutomationElement parent)
        {
            return parent.ElementsByCondition(TreeScope.Children, PropertyCondition.TrueCondition);
        }

        public static List<AutomationElement> DescendantsByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.ElementsByCondition(TreeScope.Descendants, condition);
        }

        public static List<AutomationElement> ChildrenByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.ElementsByCondition(TreeScope.Children, condition);
        }

        public static List<AutomationElement> ElementsByCondition(this AutomationElement parent, TreeScope scope,  Condition condition)
        {
            return parent.FindAll(scope, condition).Cast<AutomationElement>().ToList();
        }


        public static List<AutomationElement> Descendants(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.Elements(TreeScope.Descendants, condition);
        }


        public static List<AutomationElement> Children(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.Elements(TreeScope.Children, condition);
        }

        public static List<AutomationElement> Elements(this AutomationElement parent, TreeScope scope, Expression<Func<AutomationElement, bool>> condition)
        {
            var c = ToCondition(condition);

            return parent.FindAll(scope, c).Cast<AutomationElement>().ToList();
        }


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
            if(pattern != null && IsNull(right))
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
                  .ToDictionary(p => p.Name.RemoveRight("Property".Length), p => (AutomationProperty)p.GetValue(null)));

            var result = dic.TryGetC(propertyName);

            if (result == null)
                throw new InvalidOperationException("Property {0} is not accessible on UIAutomation queries".Formato(propertyName));

            return result;
        }
    }
}
