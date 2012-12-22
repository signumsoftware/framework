using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Signum.Utilities;
using System.Windows.Automation;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Windows.UIAutomation
{
    public static class BasicExtensions
    {
        public static void ComboSelectItem(this AutomationElement combo, Expression<Func<AutomationElement, bool>> itemCondition)
        {
            combo.Pattern<ExpandCollapsePattern>().Expand();

            var item = combo.Child(itemCondition);

            item.Pattern<SelectionItemPattern>().Select();

            combo.Pattern<ExpandCollapsePattern>().Collapse();
        }

        public static AutomationElement ComboGetSelectedItem(this AutomationElement combo)
        {
            return combo.Pattern<SelectionPattern>().Current.GetSelection().SingleOrDefault();
        }

        public static AutomationElement TabItemSelect(this AutomationElement container, string tabItemName)
        {
            var tabItem = container.Descendant(h => h.Current.ControlType == ControlType.TabItem && h.Current.Name == tabItemName);
            tabItem.Pattern<SelectionItemPattern>().Select();
            return tabItem;
        }

        public static void ButtonInvoke(this AutomationElement button)
        {
            button.Pattern<InvokePattern>().Invoke();
        }

        public static void Value(this AutomationElement element, string value)
        {
            element.Pattern<ValuePattern>().SetValue(value);
        }

        public static string Value(this AutomationElement element)
        {
            return element.Pattern<ValuePattern>().Current.Value;
        }

        public static void SetCheck(this AutomationElement element, bool isChecked)
        {
            if (isChecked)
                element.Check();
            else
                element.UnCheck();
        }

        public static void Check(this AutomationElement element)
        {
            var  ck= element.Pattern<TogglePattern>();
            if (ck.Current.ToggleState == ToggleState.Indeterminate)
                ck.Toggle();

            if(ck.Current.ToggleState != ToggleState.On)
                ck.Toggle();

            if (ck.Current.ToggleState != ToggleState.On)
                throw new InvalidOperationException("The checkbox {0} has not been checked".Formato(element.Current.AutomationId));
        }

        public static void UnCheck(this AutomationElement element)
        { 
            var ck = element.Pattern<TogglePattern>();

            if (ck.Current.ToggleState != ToggleState.Off)
                ck.Toggle();

            if (ck.Current.ToggleState != ToggleState.Off)
                throw new InvalidOperationException("The checkbox {0} has not been unchecked".Formato(element.Current.AutomationId));
        }



        public static void SelectByName(this List<AutomationElement> list, string toString, Func<string> containerDescription)
        {
            var filtered = list.Where(a => a.Current.Name == toString).ToList();

            if (filtered.Count == 1)
            {
                filtered.SingleEx().Pattern<SelectionItemPattern>().Select();
            }
            else
            {
                filtered = list.Where(a => a.Current.Name.Contains(toString, StringComparison.InvariantCultureIgnoreCase)).ToList();

                if (filtered.Count == 0)
                    throw new InvalidOperationException("No element found on {0} with ToString '{1}'. Found: \r\n{2}".Formato(containerDescription(), toString, list.ToString(a => a.Current.Name, "\r\n")));

                if (filtered.Count > 1)
                    throw new InvalidOperationException("Ambiguous elements found on {0} with ToString '{1}'. Found: \r\n{2}".Formato(containerDescription(), toString, filtered.ToString(a => a.Current.Name, "\r\n")));

                filtered.Single().Pattern<SelectionItemPattern>().Select();
            }
        }

        public static bool IsVisible(this AutomationElement element)
        {
            //return !element.Current.IsOffscreen;
            return !double.IsInfinity(element.Current.BoundingRectangle.X) && !double.IsInfinity(element.Current.BoundingRectangle.Y);
        }

        public static void WaitVisible(this AutomationElement element, Func<string> actionDescription = null, int? timeOut = null)
        {
            if (actionDescription == null)
                actionDescription = () => "Wait Visible " + element.Current.ClassName;

            element.Wait(() => element.IsVisible(), actionDescription, timeOut);
        }
    }
}
