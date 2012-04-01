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
        public static int DefaultTimeOut = 2000;
        public static int DefaultSleep = 200;

        public static void Wait(this AutomationElement automationElement, Func<bool> waitCondition)
        {
            Wait(automationElement, DefaultTimeOut, DefaultSleep, waitCondition);
        }

        public static void Wait(this AutomationElement automationElement, int timeOut, Func<bool> waitCondition)
        {
            Wait(automationElement, timeOut, DefaultSleep, waitCondition);
        }

        public static void Wait(this AutomationElement automationElement, int timeOut, int sleep, Func<bool> waitCondition)
        {
            long start = PerfCounter.Ticks;

            if (waitCondition())
                return;

            while (true)
            {
                Thread.Sleep(sleep);

                if (waitCondition())
                    return;

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > timeOut)
                    throw new TimeoutException("Wait condition failed after {0} ms".Formato(timeOut));
            }
        }



        public static AutomationElement WaitDescendant(this AutomationElement automationElement, Expression<Func<AutomationElement, bool>> condition)
        {
            return WaitElement(automationElement, DefaultTimeOut, DefaultSleep, TreeScope.Descendants, condition);
        }

        public static AutomationElement WaitDescendant(this AutomationElement automationElement, int timeOut, Expression<Func<AutomationElement, bool>> condition)
        {
            return WaitElement(automationElement, timeOut, DefaultSleep, TreeScope.Descendants, condition);
        }


        public static AutomationElement WaitChild(this AutomationElement automationElement, Expression<Func<AutomationElement, bool>> condition)
        {
            return WaitElement(automationElement, DefaultTimeOut, DefaultSleep, TreeScope.Children, condition);
        }

        public static AutomationElement WaitChild(this AutomationElement automationElement, int timeOut, Expression<Func<AutomationElement, bool>> condition)
        {
            return WaitElement(automationElement, timeOut, DefaultSleep, TreeScope.Children, condition);
        }


        public static AutomationElement WaitElement(this AutomationElement automationElement, TreeScope scope, Expression<Func<AutomationElement, bool>> condition)
        {
            return WaitElement(automationElement, DefaultTimeOut, DefaultSleep, scope, condition);
        }

        public static AutomationElement WaitElement(this AutomationElement automationElement, int timeOut, TreeScope scope, Expression<Func<AutomationElement, bool>> condition)
        {
            return WaitElement(automationElement, timeOut, DefaultSleep, scope, condition);
        }

        public static AutomationElement WaitElement(this AutomationElement automationElement, int timeOut, int sleep, TreeScope scope, Expression<Func<AutomationElement, bool>> condition)
        {
            long start = PerfCounter.Ticks;

            var cond = FindExtensions.ToCondition(condition);

            var result = automationElement.FindFirst(scope, cond);
            if (result != null)
                return result;

            while (true)
            {
                Thread.Sleep(sleep);

                result = automationElement.FindFirst(scope, cond);
                if (result != null)
                    return result;

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > timeOut)
                    throw new TimeoutException("Element not found after {0} ms: {1}".Formato(timeOut, ExpressionEvaluator.PartialEval(condition).NiceToString()));
            }
        }



        public static AutomationElement WaitDescendantById(this AutomationElement automationElement, string automationId)
        {
            return WaitElementById(automationElement, DefaultTimeOut, DefaultSleep, TreeScope.Descendants, automationId);
        }

        public static AutomationElement WaitDescendantById(this AutomationElement automationElement, int timeOut, string automationId)
        {
            return WaitElementById(automationElement, timeOut, DefaultSleep, TreeScope.Descendants, automationId);
        }


        public static AutomationElement WaitChildById(this AutomationElement automationElement, string automationId)
        {
            return WaitElementById(automationElement, DefaultTimeOut, DefaultSleep, TreeScope.Children, automationId);
        }

        public static AutomationElement WaitChildById(this AutomationElement automationElement, int timeOut, string automationId)
        {
            return WaitElementById(automationElement, timeOut, DefaultSleep, TreeScope.Children, automationId);
        }


        public static AutomationElement WaitElementById(this AutomationElement automationElement, TreeScope scope, string automationId)
        {
            return WaitElementById(automationElement, DefaultTimeOut, DefaultSleep, scope, automationId);
        }

        public static AutomationElement WaitElementById(this AutomationElement automationElement, int timeOut, TreeScope scope, string automationId)
        {
            return WaitElementById(automationElement, timeOut, DefaultSleep, scope, automationId);
        }

        public static AutomationElement WaitElementById(this AutomationElement automationElement, int timeOut, int sleep, TreeScope scope, string automationId)
        {
            long start = PerfCounter.Ticks;

            var cond = new PropertyCondition(AutomationElement.AutomationIdProperty, automationElement);

            var result = automationElement.FindFirst(scope, cond);
            if (result != null)
                return result;

            while (true)
            {
                Thread.Sleep(sleep);

                result = automationElement.FindFirst(scope, cond);
                if (result != null)
                    return result;

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > timeOut)
                    throw new InvalidOperationException("Element not foud after {0} ms: AutomationID = ".Formato(timeOut, automationId));
            }
        }



        public static AutomationElement WaitDescendantByCondition(this AutomationElement automationElement, Condition condition)
        {
            return WaitElementByCondition(automationElement, DefaultTimeOut, DefaultSleep, TreeScope.Descendants, condition);
        }

        public static AutomationElement WaitDescendantByCondition(this AutomationElement automationElement, int timeOut, Condition condition)
        {
            return WaitElementByCondition(automationElement, timeOut, DefaultSleep, TreeScope.Descendants, condition);
        }


        public static AutomationElement WaitChildByCondition(this AutomationElement automationElement, Condition condition)
        {
            return WaitElementByCondition(automationElement, DefaultTimeOut, DefaultSleep, TreeScope.Children, condition);
        }

        public static AutomationElement WaitChildByCondition(this AutomationElement automationElement, int timeOut, Condition condition)
        {
            return WaitElementByCondition(automationElement, timeOut, DefaultSleep, TreeScope.Children, condition);
        }


        public static AutomationElement WaitElementByCondition(this AutomationElement automationElement, TreeScope scope, Condition condition)
        {
            return WaitElementByCondition(automationElement, DefaultTimeOut, DefaultSleep, scope, condition);
        }

        public static AutomationElement WaitElementByCondition(this AutomationElement automationElement, int timeOut, TreeScope scope, Condition condition)
        {
            return WaitElementByCondition(automationElement, timeOut, DefaultSleep, scope, condition);
        }

        public static AutomationElement WaitElementByCondition(this AutomationElement automationElement, int timeOut, int sleep, TreeScope scope, Condition condition)
        {
            long start = PerfCounter.Ticks;

            var result = automationElement.FindFirst(scope, condition);
            if (result != null)
                return result;

            while (true)
            {
                Thread.Sleep(sleep);

                result = automationElement.FindFirst(scope, condition);
                if (result != null)
                    return result;

                if (((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds) > timeOut)
                    throw new InvalidOperationException("Element not foud after {0} ms: AutomationID = ".Formato(timeOut, condition.NiceToString()));
            }
        }
    }
}
