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
        #region TryElementByCondition
        public static AutomationElement TryDescendantByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.TryElementByCondition(TreeScope.Descendants, condition);
        }

        public static AutomationElement TryChildByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.TryElementByCondition(TreeScope.Children, condition);
        }

        public static AutomationElement TryParentByCondition(this AutomationElement child, Condition condition)
        {
            return child.TryElementByCondition(TreeScope.Parent, condition);
        }

        public static AutomationElement TryElementByCondition(this AutomationElement element, TreeScope scope, Condition condition)
        {
            var result = scope == TreeScope.Parent || scope == TreeScope.Ancestors ?
                        new TreeWalker(condition).GetParent(element) :
                        element.FindFirst(scope, condition);

            return result;
        } 
        #endregion

        #region TryElementById
        public static AutomationElement TryDescendantById(this AutomationElement parent, string automationId)
        {
            return parent.TryElementById(TreeScope.Descendants, automationId);
        }

        public static AutomationElement TryChildById(this AutomationElement parent, string automationId)
        {
            return parent.TryElementById(TreeScope.Children, automationId);
        }

        public static AutomationElement TryParentById(this AutomationElement parent, string automationId)
        {
            return parent.TryElementById(TreeScope.Children, automationId);
        }

        public static AutomationElement TryElementById(this AutomationElement element, TreeScope scope, string automationId)
        {
            return element.TryElementByCondition(scope, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
        }
        
        #endregion

        #region TryElement
        public static AutomationElement TryDescendant(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.TryElement(TreeScope.Descendants, condition);
        }

        public static AutomationElement TryChild(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.TryElement(TreeScope.Children, condition);
        }

        public static AutomationElement TryParent(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.TryElement(TreeScope.Parent, condition);
        }

        public static AutomationElement TryElement(this AutomationElement element, TreeScope scope, Expression<Func<AutomationElement, bool>> condition)
        {
            var c = ConditionBuilder.ToCondition(condition);

            return element.TryElementByCondition(scope, c);
        }
        
        #endregion

        #region ElementByCondition
        public static AutomationElement DescendantByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.ElementByCondition(TreeScope.Descendants, condition);
        }

        public static AutomationElement ChildByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.ElementByCondition(TreeScope.Children, condition);
        }

        public static AutomationElement ParentByCondition(this AutomationElement parent, Condition condition)
        {
            return parent.ElementByCondition(TreeScope.Children, condition);
        }

        public static AutomationElement ElementByCondition(this AutomationElement element, TreeScope scope, Condition condition)
        {
            var result = element.TryElementByCondition(scope, condition);

            if (result == null)
                throw new ElementNotFoundException("No AutomationElement found: {0}".FormatWith(condition.NiceToString()));

            return result;
        } 
        #endregion

        #region ElementById
        public static AutomationElement DescendantById(this AutomationElement parent, string automationId)
        {
            return parent.ElementById(TreeScope.Descendants, automationId);
        }

        public static AutomationElement ChildById(this AutomationElement parent, string automationId)
        {
            return parent.ElementById(TreeScope.Children, automationId);
        }

        public static AutomationElement ParentById(this AutomationElement child, string automationId)
        {
            return child.ElementById(TreeScope.Parent, automationId);
        }

        public static AutomationElement ElementById(this AutomationElement element, TreeScope scope, string automationId)
        {
            var c = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);

            return element.ElementByCondition(scope, c);
        } 
        #endregion

        #region Element
        public static AutomationElement Descendant(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.Element(TreeScope.Descendants, condition);
        }

        public static AutomationElement Child(this AutomationElement parent, Expression<Func<AutomationElement, bool>> condition)
        {
            return parent.Element(TreeScope.Children, condition);
        }

        public static AutomationElement Parent(this AutomationElement child, Expression<Func<AutomationElement, bool>> condition)
        {
            return child.Element(TreeScope.Parent, condition);
        }

        public static AutomationElement Element(this AutomationElement element, TreeScope scope, Expression<Func<AutomationElement, bool>> condition)
        {
            var c = ConditionBuilder.ToCondition(condition);

            return element.ElementByCondition(scope, c);
        }

        #endregion

        public static AutomationElement Child(this AutomationElement parent)
        {
            return parent.ElementByCondition(TreeScope.Children, Condition.TrueCondition);
        }

        public static AutomationElement Parent(this AutomationElement child)
        {
            return child.ElementByCondition(TreeScope.Parent, Condition.TrueCondition);
        }

        #region ElementsByCondition
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
        #endregion

        #region Elements
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
        
        #endregion

        public static List<AutomationElement> DescendantsAll(this AutomationElement parent)
        {
            return parent.ElementsByCondition(TreeScope.Descendants, PropertyCondition.TrueCondition);
        }

        public static List<AutomationElement> ChildrenAll(this AutomationElement parent)
        {
            return parent.ElementsByCondition(TreeScope.Children, PropertyCondition.TrueCondition);
        }

    }

    [Serializable]
    public class ElementNotFoundException : Exception
    {
        public ElementNotFoundException() { }
        public ElementNotFoundException(string message) : base(message) { }
        public ElementNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected ElementNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
