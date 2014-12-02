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
using System.Reflection;

namespace Signum.Windows.UIAutomation
{
    public static class WaitExtensions
    {
        public static int DefaultSleep = 200;
        public static int DefaultTimeout = 2 * 1000;

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

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > (timeOut ?? DefaultTimeout))
                    throw new TimeoutException("Wait condition failed after {0} ms: ".FormatWith(timeOut ?? DefaultTimeout) + (actionDescription == null ? null : actionDescription()));
            }
        }

        public static void WaitDataContextChangedAfter(this AutomationElement element, Action action, int? timeOut = null, Func<string> actionDescription = null)
        {
            string oldValue = element.Current.ItemStatus;
            if (string.IsNullOrEmpty(oldValue))
                throw new InvalidOperationException("Element does not has ItemStatus set. Consider setting m:Common.AutomationItemStatusFromDataContext on the WPF control");

            var pid = element.Current.ProcessId;
            int c = 0;
            var previous = GetAllProcessWindows(pid, c).Select(a => a.GetRuntimeId().ToString(".")).ToHashSet();
            c++;
            action();

            if (actionDescription == null)
                actionDescription = () => "DataContextChanged for {0}".FormatWith(oldValue);
         
            element.Wait(() =>
            {
                var newValue = element.Current.ItemStatus;
                if (newValue != null && newValue != oldValue)
                    return true;

                var newWindow = GetAllProcessWindows(pid, c).FirstOrDefault(a => !previous.Contains(a.GetRuntimeId().ToString(".")));
                c++;
                MessageBoxProxy.ThrowIfError(newWindow);

                return false;
            }, actionDescription, timeOut);
        }

        public static void WaitDataContextSet(this AutomationElement element, Func<string> actionDescription = null, int? timeOut = null)
        {
            if (actionDescription == null)
                actionDescription = () => "Has DataContext";

            element.Wait(() =>
            {
                var newValue = element.Current.ItemStatus;
                if (newValue.HasText())
                    return true;

                element.AssertMessageBoxChild();

                return false;
            }, actionDescription, timeOut);
        }

        public static int CapturaWindowTimeout = 5 * 1000;


        public static AutomationElement CaptureWindow(this AutomationElement element, Action action, Func<string> actionDescription = null, int? timeOut = null, Func<AutomationElement, bool> windowsCondition = null)
        {
            if (actionDescription == null)
                actionDescription = () => "Get Windows after";

            var pid = element.Current.ProcessId;
            int c = 0;
            var previous = GetAllProcessWindows(pid, c).Select(a => a.GetRuntimeId().ToString(".")).ToHashSet();
            c++;
            action();

            AutomationElement newWindow = null;

            element.Wait(() =>
            {
                newWindow = GetAllProcessWindows(pid, c).FirstOrDefault(a => !previous.Contains(a.GetRuntimeId().ToString(".")) && 
                    (windowsCondition == null || windowsCondition(a)));
                c++;
                MessageBoxProxy.ThrowIfError(newWindow);

                if (newWindow != null)
                    return true;

                return false;
            }, actionDescription, timeOut ?? CapturaWindowTimeout);

            return newWindow;
        }

        public static List<AutomationElement> GetAllProcessWindows(int processId, int count = -10)
        {
            Entries.Add(new LogEntry
            {
                Count = count,
                retryCount = -1,
                StackTrace = Environment.StackTrace
            });

            int retryCount = 0;
        retry:
            var result = GetRecursiveProcessWindows(AutomationElement.RootElement, processId);

            Entries.AddRange(result.Select(r => new LogEntry
            {
                Count = count,
                retryCount = retryCount,
                ClassName = r.SafeGet(p => p.ClassName, "error"),
                Name = r.SafeGet(p =>p.Name, "error"),
                RuntimeId = r.GetRuntimeId().ToString("."),
                ItemStatus = r.SafeGet(p => p.ItemStatus, "error"),
            }));

            if (result.IsEmpty() || result.Any(a => a.SafeGet(p => p.ClassName.StartsWith("HwndWrapper"), false)))
            {
                if (retryCount > 4)
                    throw new InvalidOperationException("No windows found after {0} retries".FormatWith(retryCount));

                retryCount++;
                goto retry;
            }

            return result;
        }

        class LogEntry
        {
            public int Count;
            public int retryCount;
            public string ClassName;
            public string Name;
            public string RuntimeId;
            public string ItemStatus;
            public string StackTrace;
        }

        public static T SafeGet<T>(this AutomationElement element, Func<AutomationElement.AutomationElementInformation, T> getter, T error)
        {
            try
            {
                return getter(element.Current);
            }
            catch (ElementNotAvailableException)
            {
                return error;
            }
        }

        static List<LogEntry> Entries = new List<LogEntry>();

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

        public static AutomationElement CaptureChildWindow(this AutomationElement element, Action action, Func<string> actionDescription = null, int? timeOut = null)
        {
            if (actionDescription == null)
                actionDescription = () => "Get Windows after";

            var parentWindow = WindowProxy.Normalize(element);

            var previous = parentWindow.Children(a=>a.Current.ControlType == ControlType.Window).Select(a => a.GetRuntimeId().ToString(".")).ToHashSet();

            action();

            AutomationElement newWindow = null;

            List<AutomationElement> currentWindows = new List<AutomationElement>(); 

            element.Wait(() =>
            {
                currentWindows = parentWindow.Children(a => a.Current.ControlType == ControlType.Window);
                newWindow = currentWindows.FirstOrDefault(a => !previous.Contains(a.GetRuntimeId().ToString(".")));

                MessageBoxProxy.ThrowIfError(newWindow);

                if (newWindow != null )
                    return true;


                return false;
            }, () => actionDescription() + 
                "\r\n\tcurrentWindows: " + currentWindows.ToString(a => NiceToString(a), "\r\n\t\t") + 
                "\r\n\tnewWindow: " + NiceToString(newWindow) + 
                "\r\n\tprevious: " + previous.ToString(a=>a, " ")            
            , timeOut ?? CapturaWindowTimeout);
            return newWindow;
        }

        public static string NiceToString(AutomationElement ae)
        {
            if (ae == null)
                return "NULL";

            return "{0} [{1}] ({2})".FormatWith(ae.Current.Name, ae.Current.ClassName, ae.GetRuntimeId().ToString("."));
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

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > (timeOut ?? DefaultTimeout))
                    throw new TimeoutException("Element not found after {0} ms: {1}".FormatWith((timeOut ?? DefaultTimeout), ExpressionEvaluator.PartialEval(condition).ToString()));
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

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > (timeOut ?? DefaultTimeout))
                    throw new InvalidOperationException("Element not foud after {0} ms: AutomationID = {1}".FormatWith((timeOut ?? DefaultTimeout), automationId));
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

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > (timeOut ?? DefaultTimeout))
                    throw new InvalidOperationException("Element not foud after {0} ms: {1}".FormatWith((timeOut ?? DefaultTimeout), condition.NiceToString()));
            }
        }
    }
}
