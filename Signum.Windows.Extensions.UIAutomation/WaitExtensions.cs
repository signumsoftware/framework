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

        public static void WaitDataContextChanged(this AutomationElement automationElement, int? timeOut = null)
        {
            string oldValue = automationElement.Current.HelpText;

            if (string.IsNullOrEmpty(oldValue))
                throw new InvalidOperationException("Element does not has HelpText set. Consider setting m:Common.AutomationHelpTextFromDataContext on the WPF control");

            automationElement.Wait(() =>
            {
                var newValue = automationElement.Current.HelpText;
                return newValue != null && newValue != oldValue;
            }, () => "DataContextChanged for {0}".Formato(oldValue), timeOut);
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
