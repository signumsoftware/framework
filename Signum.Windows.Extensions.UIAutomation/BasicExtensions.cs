using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Signum.Utilities;
using System.Windows.Automation;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;

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
            var tabItem = container.TryDescendant(h => h.Current.ControlType == ControlType.TabItem && h.Current.Name == tabItemName);
            if (tabItem == null)
                tabItem = container.Descendants(h => h.Current.ControlType == ControlType.TabItem)
                    .Where(el => el.TryChild(h => h.Current.ControlType == ControlType.Text && h.Current.Name == tabItemName) != null)
                    .FirstEx(() => "TabItem {0} not found".Formato(tabItemName));

            tabItem.Pattern<SelectionItemPattern>().Select();
            return tabItem;
        }

        public static void ButtonInvoke(this AutomationElement button)
        {
            button.Pattern<InvokePattern>().Invoke();
        }

        public static AutomationElement ButtonInvokeCapture(this AutomationElement button, Func<string> actionDescription = null, int? timeOut = null)
        {
            return button.CaptureWindow(() => button.ButtonInvoke(), actionDescription, timeOut);
        }

        public static AutomationElement ButtonInvokeCaptureChild(this AutomationElement button, Func<string> actionDescription = null, int? timeOut = null)
        {
            return button.CaptureChildWindow(() => button.ButtonInvoke(), actionDescription, timeOut);
        }

        public static void Value(this AutomationElement element, string value)
        {
            element.Pattern<ValuePattern>().SetValue(value ?? "");
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

        public static bool? GetCheckState(this AutomationElement element)
        {
            var ck = element.Pattern<TogglePattern>();
            switch (ck.Current.ToggleState)
            {
                case ToggleState.Off:
                    return false;
                case ToggleState.On:
                    return true;
                case ToggleState.Indeterminate:
                default:
                    return null;
            }
        }

        public static void Check(this AutomationElement element)
        {
            var ck = element.Pattern<TogglePattern>();
            if (ck.Current.ToggleState == ToggleState.Indeterminate)
                ck.Toggle();

            if (ck.Current.ToggleState != ToggleState.On)
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

        public static void WaitComboBoxHasItems(this AutomationElement comboBox, Func<string> containerDescription = null, int? timeOut = null)
        {
            if (containerDescription == null)
                containerDescription = () => "ComboBox has items";

            comboBox.Wait(() =>
            {
                comboBox.Pattern<ExpandCollapsePattern>().Expand();
                return comboBox.TryChild(c => c.Current.ControlType == ControlType.ListItem) != null;
            }, containerDescription, timeOut);
        }

        public static void SelectListItemByName(this AutomationElement listParent, string toString, Func<string> containerDescription)
        {
            var only = listParent.TryChild(a => a.Current.Name == toString);
            if (only != null)
            {
                only.Pattern<SelectionItemPattern>().Select();
            }
            else
            {
                var list = listParent.ChildrenAll();

                var filtered = list.Where(a => a.Current.Name.Contains(toString, StringComparison.InvariantCultureIgnoreCase)).ToList();

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

        public static AutomationElement AssertClassName(this AutomationElement element, string expectedType)
        {
            if (element.Current.ClassName == expectedType)
                return element;

            throw new InvalidCastException("The AutomationElement is not a {0}, but a {1}".Formato(expectedType, element.Current.ClassName));
        }
    }
}
