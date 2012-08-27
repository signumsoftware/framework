using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
    public class SelectorWindowProxy: WindowProxy
    {
        public SelectorWindowProxy(AutomationElement element)
            : base(element)
        {
        }

        public void Check(string value)
        {
            Element.Child(a => a.Current.ControlType == ControlType.Button && a.Current.ItemStatus == value).Check();
        }

        public void Check<T>() where T : IdentifiableEntity
        {
            Check(typeof(T).FullName); 
        }

        public void Check(Type type)
        {
            Check(type.FullName);
        }

        public AutomationElement CheckCapture(string value, int? timeout = null)
        {
            return Element.CaptureWindow(
                () => Check(value),
                () => "select {0} on selector window".Formato(value), timeout);
        }

        public NormalWindowProxy<T> CheckCapture<T>(int? timeout = null) where T : IdentifiableEntity
        {
            return new NormalWindowProxy<T>(CheckCapture( typeof(T).FullName, timeout));
        }

        public AutomationElement CheckCapture(Type type, int? timeout = null)
        {
            return CheckCapture(type.FullName, timeout);
        }



        public static void Select(AutomationElement element, string value)
        {
            using (var selector = new SelectorWindowProxy(element))
            {
                selector.Check(value);
            }
        }

        public static void Select<T>(AutomationElement element) where T : IdentifiableEntity
        {
            Select(element, typeof(T).FullName);
        }

        public static void Select(AutomationElement element, Type type)
        {
            Select(element, type.FullName);
        }


        public static AutomationElement SelectCapture(AutomationElement element, string value, int? timeout = null)
        {
            using (var selector = new SelectorWindowProxy(element))
                return selector.CheckCapture(value, timeout);
        }

        public static NormalWindowProxy<T> SelectCapture<T>(AutomationElement element, int? timeout = null) where T : IdentifiableEntity
        {
            return new NormalWindowProxy<T>(SelectCapture(element, typeof(T).FullName, timeout));
        }

        public static AutomationElement SelectCapture(AutomationElement element, Type type, int? timeout = null)
        {
            return SelectCapture(element, type.FullName, timeout);
        }
    }
}
