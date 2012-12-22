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

        public static AutomationElement TryNormalizeById(this AutomationElement parent, string automationId)
        {
            TreeWalker tw = new TreeWalker(new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));

            return tw.Normalize(parent);
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

        public static AutomationElement NormalizeById(this AutomationElement parent, string automationId)
        {
            TreeWalker tw = new TreeWalker(new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));

            var result = tw.Normalize(parent);

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
            return parent.FindFirst(scope, condition);
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

        public static AutomationElement NormalizeByCondition(this AutomationElement parent, Condition condition)
        {
            TreeWalker tw = new TreeWalker(condition);

            var result = tw.Normalize(parent);

            if (result == null)
                throw new KeyNotFoundException("No AutomationElement found with AutomationID '{0}'".Formato(condition.NiceToString()));

            return result;
        }

        public static List<AutomationElement> DescendantsByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.ElementsByCondition(TreeScope.Descendants, condition);
        }

        public static List<AutomationElement> ChildrenByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.ElementsByCondition(TreeScope.Children, condition);
        }

        public static List<AutomationElement> ElementsByCondition(this AutomationElement parent, TreeScope scope, Condition condition)
        {
            return parent.FindAll(scope, condition).Cast<AutomationElement>().ToList();
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
            return parent.FindFirst(scope, ConditionBuilder.ToCondition(condition));
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
            var c = ConditionBuilder.ToCondition(condition); 

            var result = parent.FindFirst(scope, c);

            if (result == null)
                throw new KeyNotFoundException("No AutomationElement found with condition {0}".Formato(ExpressionEvaluator.PartialEval(condition).NiceToString()));

            return result;
        }

        public static AutomationElement Normalize(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            var c = ConditionBuilder.ToCondition(condition);

            TreeWalker tw = new TreeWalker(c);

            var result = tw.Normalize(parent);

            if (result == null)
                throw new KeyNotFoundException("No AutomationElement found with AutomationID '{0}'".Formato(condition.NiceToString()));

            return result;
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
            var c = ConditionBuilder.ToCondition(condition);

            return parent.FindAll(scope, c).Cast<AutomationElement>().ToList();
        }

        public static List<AutomationElement> DescendantsAll(this AutomationElement parent)
        {
            return parent.ElementsByCondition(TreeScope.Descendants, PropertyCondition.TrueCondition);
        }

        public static List<AutomationElement> ChildrenAll(this AutomationElement parent)
        {
            return parent.ElementsByCondition(TreeScope.Children, PropertyCondition.TrueCondition);
        }

    }
}
