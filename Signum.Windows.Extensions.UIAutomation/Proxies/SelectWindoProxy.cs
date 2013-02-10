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
            element.AssertClassName("SelectorWindow");
        }

        public void Select(string value)
        {
            Element.Child(a => a.Current.ControlType == ControlType.Button && a.Current.Name == value).Check();
        }

        public void Select<T>() where T : IdentifiableEntity
        {
            Select(typeof(T).FullName); 
        }

        public AutomationElement SelectCapture(string value, int? timeout = null)
        {
            return Element.CaptureWindow(
                () => Select(value),
                () => "select {0} on selector window".Formato(value), timeout);
        }

        public NormalWindowProxy<T> SelectCapture<T>(int? timeout = null) where T : IdentifiableEntity
        {
            return new NormalWindowProxy<T>(SelectCapture(typeof(T).FullName, timeout));
        }


        public static void Select(AutomationElement element, string value)
        {
            using (var selector = new SelectorWindowProxy(element))
            {
                selector.Select(value);
            }
        }

        public static void Select<T>(AutomationElement element) where T : IdentifiableEntity
        {
            Select(element, typeof(T).FullName);
        }

        public static AutomationElement SelectCapture(AutomationElement element, string value)
        {
            using (var selector = new SelectorWindowProxy(element))
            {
                return selector.SelectCapture(value);
            }
        }

        public static NormalWindowProxy<T> SelectCapture<T>(AutomationElement element) where T : IdentifiableEntity
        {
            using (var selector = new SelectorWindowProxy(element))
            {
                return selector.SelectCapture<T>();
            }
        }
    }
}
