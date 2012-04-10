using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities;

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
    }
}
