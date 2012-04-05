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
        }

        public static AutomationElement ComboGetSelectedItem(this AutomationElement combo)
        {
            return combo.Pattern<SelectionPattern>().Current.GetSelection().SingleOrDefault();
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


        public static AutomationElement GetWindowAfter(this AutomationElement element, Action action, Func<string> actionDescription, int? timeOut = null)
        {
            var previous = AutomationElement.RootElement.Children(a => a.Current.ProcessId == element.Current.ProcessId).Select(a => a.GetRuntimeId().ToString(".")).ToHashSet();

            action();

            AutomationElement newWindow = null;

            element.Wait(() =>
            {
                newWindow = AutomationElement.RootElement
                    .Children(a => a.Current.ProcessId == element.Current.ProcessId)
                    .FirstOrDefault(a => !previous.Contains(a.GetRuntimeId().ToString(".")));

                return newWindow != null;
            }, actionDescription, timeOut ?? 5000);
            return newWindow;
        }

        public static AutomationElement GetModalWindowAfter(this AutomationElement element, Action action, Func<string> actionDescription, int? timeOut = null)
        {
            TreeWalker walker = new TreeWalker(ConditionBuilder.ToCondition(a => a.Current.ControlType == ControlType.Window));

            var parentWindow = walker.Normalize(element);

            action();

            AutomationElement newWindow = null;

            element.Wait(() =>
            {
                newWindow = walker.GetFirstChild(parentWindow);

                return newWindow != null;
            }, actionDescription, timeOut ?? 5000);
            return newWindow;
        }
    }
}
