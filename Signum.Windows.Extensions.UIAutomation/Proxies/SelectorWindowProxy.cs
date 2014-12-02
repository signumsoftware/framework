using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{


    public class SelectorWindowProxy : WindowProxy
    {
        public SelectorWindowProxy(AutomationElement element)
            : base(element.AssertClassName("SelectorWindow"))
        {
        }

        public void Select(string value)
        {
            Element.Child(a => a.Current.ControlType == ControlType.Button && a.Current.Name == value).Check();
        }

        public void Select(Type type)
        {
            Select(type.FullName);
        }

        public void Select<T>() where T : Entity
        {
            Select(typeof(T).FullName);
        }

        public AutomationElement SelectCapture(string value, int? timeout = null)
        {
            return Element.CaptureWindow(
                () => Select(value),
                () => "select {0} on selector window".FormatWith(value), timeout);
        }

        public AutomationElement SelectCapture(Type type, int? timeout = null)
        {
            return SelectCapture(type.FullName, timeout);
        }

        public NormalWindowProxy<T> SelectCapture<T>(int? timeout = null) where T : Entity
        {
            return new NormalWindowProxy<T>(SelectCapture(typeof(T).FullName, timeout));
        }
    }

    public static class SelectorWindowExtensions
    {
        public static SelectorWindowProxy ToSelectorWindow(this AutomationElement element)
        {
            return new SelectorWindowProxy(element);
        }
    }
}
