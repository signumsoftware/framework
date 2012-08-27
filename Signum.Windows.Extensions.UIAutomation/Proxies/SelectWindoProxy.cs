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

        public void SelectOption(string value)
        {
            Element.Child(a => a.Current.ControlType == ControlType.Button && a.Current.ItemStatus == value).ButtonInvoke();
        }

        public void SelectType<T>() where T: IdentifiableEntity
        {
            SelectOption(typeof(T).FullName); 
        }

        public void SelectType(Type type)
        {
            SelectOption(type.FullName);
        }

        public AutomationElement SelectOptionWindow(string value, int? timeout = null)
        {
            return this.GetWindowAfter(
                () => SelectOption(value),
                () => "select {0} on selector window".Formato(value), timeout);
        }

        public NormalWindowProxy<T> SelectTypeWindow<T>(int? timeout = null) where T : IdentifiableEntity
        {
            var element = this.GetWindowAfter(
               () => SelectOption(typeof(T).FullName),
               () => "select {0} on type selector window".Formato(typeof(T)), timeout);

            return new NormalWindowProxy<T>(element);
        }

        public AutomationElement SelectTypeWindow(Type type, int? timeout = null)
        {
            return this.GetWindowAfter(
                () => SelectOption(type.FullName),
                () => "select {0} on type selector window".Formato(type), timeout);
        }
    }
}
