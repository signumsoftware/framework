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
    public static class WaitExtensions
    {
        public static int DefaultSleep = 200;
        public static int DefaultTimeOut = 2 * 1000;

        public static void Wait(this AutomationElement automationElement, Func<bool> waitCondition, Func<string> actionDescription, int? timeOut = null)
        {
            long start = PerfCounter.Ticks;

            if (waitCondition())
                return;

            while (true)
            {
                Thread.Sleep(DefaultSleep);

                if (waitCondition())
                    return;

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > (timeOut ?? DefaultTimeOut))
                    throw new TimeoutException("Wait condition failed after {0} ms: ".Formato(timeOut ?? DefaultTimeOut) + actionDescription == null ? null : actionDescription());
            }
        }

        public static void WaitDataContextChangedAfter(this AutomationElement element, Action action, int? timeOut = null, Func<string> actionDescription = null)
        {
            string oldValue = element.Current.HelpText;
            if (string.IsNullOrEmpty(oldValue))
                throw new InvalidOperationException("Element does not has HelpText set. Consider setting m:Common.AutomationHelpTextFromDataContext on the WPF control");

            action();

            if (actionDescription == null)
                actionDescription = () => "DataContextChanged for {0}".Formato(oldValue);

            element.Wait(() =>
            {
                var newValue = element.Current.HelpText;
                if (newValue != null && newValue != oldValue)
                    return true;

                element.AssertMessageBoxChild();

                return false;
            }, actionDescription, timeOut);
        }

        public static void WaitDataContextSet(this AutomationElement element, Func<string> actionDescription = null, int? timeOut = null)
        {
            if (actionDescription == null)
                actionDescription = () => "Has DataContext";

            element.Wait(() =>
            {
                var newValue = element.Current.HelpText;
                if (newValue.HasText())
                    return true;

                element.AssertMessageBoxChild();

                return false;
            }, actionDescription, timeOut);
        }

        public static int CapturaWindowTimeout = 5 * 1000;

        public static AutomationElement CaptureWindow(this AutomationElement element, Action action, Func<string> actionDescription = null, int? timeOut = null)
        {
            if (actionDescription == null)
                actionDescription = () => "Get Windows after";

            var previous = GetAllProcessWindows(element).Select(a => a.GetRuntimeId().ToString(".")).ToHashSet();

            bool bla = element.Current.IsPassword;

            action();

            AutomationElement newWindow = null;

            element.Wait(() =>
            {
                newWindow = GetAllProcessWindows(element).FirstOrDefault(a => !previous.Contains(a.GetRuntimeId().ToString(".")));

                MessageBoxProxy.AssertNoErrorWindow(newWindow);

                if (newWindow != null)
                    return true;

                return false;
            }, actionDescription, timeOut ?? CapturaWindowTimeout);

            return newWindow;
        }

        public static List<AutomationElement> GetAllProcessWindows(AutomationElement element)
        {
            return GetRecursiveProcessWindows(AutomationElement.RootElement, element.Current.ProcessId);
        }

        static List<AutomationElement> GetRecursiveProcessWindows(AutomationElement parentWindow, int processId)
        {
            var children = parentWindow.Children(a => a.Current.ProcessId == processId && a.Current.ControlType == ControlType.Window);
         
            List<AutomationElement> result = new List<AutomationElement>();
            foreach (var child in children)
            {
                result.Add(child);
                result.AddRange(GetRecursiveProcessWindows(child, processId));
            }

            return result;
        }

        public static AutomationElement CaptureChildWindow(this AutomationElement element, Action action, Func<string> actionDescription, int? timeOut = null)
        {
            var parentWindow = WindowProxy.Normalize(element);

            action();

            AutomationElement newWindow = null;

            element.Wait(() =>
            {
                newWindow = parentWindow.TryChild(a => a.Current.ControlType == ControlType.Window);

                MessageBoxProxy.AssertNoErrorWindow(newWindow);

                if (newWindow != null )
                    return true;


                return false;
            }, actionDescription, timeOut ?? CapturaWindowTimeout);
            return newWindow;
        }

        public static AutomationElement WaitDescendant(this AutomationElement automationElement, Expression<Func<AutomationElement, bool>> condition, int? timeOut = null)
        {
            return WaitElement(automationElement, TreeScope.Descendants, condition, timeOut);
        }

        public static AutomationElement WaitChild(this AutomationElement automationElement, Expression<Func<AutomationElement, bool>> condition, int? timeOut = null)
        {
            return WaitElement(automationElement, TreeScope.Children, condition, timeOut);
        }

        public static AutomationElement WaitElement(this AutomationElement automationElement, TreeScope scope, Expression<Func<AutomationElement, bool>> condition, int? timeOut = null)
        {
            long start = PerfCounter.Ticks;

            var cond = ConditionBuilder.ToCondition(condition);

            var result = automationElement.FindFirst(scope, cond);
            if (result != null)
                return result;

            while (true)
            {
                Thread.Sleep(DefaultSleep);

                result = automationElement.FindFirst(scope, cond);
                if (result != null)
                    return result;

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > (timeOut ?? DefaultTimeOut))
                    throw new TimeoutException("Element not found after {0} ms: {1}".Formato((timeOut ?? DefaultTimeOut), ExpressionEvaluator.PartialEval(condition).NiceToString()));
            }
        }



        public static AutomationElement WaitDescendantById(this AutomationElement automationElement, string automationId, int? timeOut = null)
        {
            return WaitElementById(automationElement, TreeScope.Descendants, automationId, timeOut);
        }

        public static AutomationElement WaitChildById(this AutomationElement automationElement, string automationId, int? timeOut = null)
        {
            return WaitElementById(automationElement, TreeScope.Children, automationId, timeOut);
        }

        public static AutomationElement WaitElementById(this AutomationElement automationElement, TreeScope scope, string automationId, int? timeOut = null)
        {
            long start = PerfCounter.Ticks;

            var cond = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);

            var result = automationElement.FindFirst(scope, cond);
            if (result != null)
                return result;

            while (true)
            {
                Thread.Sleep(DefaultSleep);

                result = automationElement.FindFirst(scope, cond);
                if (result != null)
                    return result;

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > (timeOut ?? DefaultTimeOut))
                    throw new InvalidOperationException("Element not foud after {0} ms: AutomationID = ".Formato((timeOut ?? DefaultTimeOut), automationId));
            }
        }




        public static AutomationElement WaitDescendantByCondition(this AutomationElement automationElement, Condition condition, int? timeOut = null)
        {
            return WaitElementByCondition(automationElement, TreeScope.Descendants, condition, timeOut);
        }

        public static AutomationElement WaitChildByCondition(this AutomationElement automationElement, Condition condition, int? timeOut = null)
        {
            return WaitElementByCondition(automationElement, TreeScope.Children, condition, timeOut);
        }

        public static AutomationElement WaitElementByCondition(this AutomationElement automationElement, TreeScope scope, Condition condition, int? timeOut = null)
        {
            long start = PerfCounter.Ticks;

            var result = automationElement.FindFirst(scope, condition);
            if (result != null)
                return result;

            while (true)
            {
                Thread.Sleep(DefaultSleep);

                result = automationElement.FindFirst(scope, condition);
                if (result != null)
                    return result;

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > (timeOut ?? DefaultTimeOut))
                    throw new InvalidOperationException("Element not foud after {0} ms: AutomationID = ".Formato((timeOut ?? DefaultTimeOut), condition.NiceToString()));
            }
        }
    }
}
