using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
    public class ValueLineWindowProxy<T> : WindowProxy
    {
        ValueLineProxy ValueLine { get; set; }


        public ValueLineWindowProxy(AutomationElement element):base(element)
        {
            ValueLine = new ValueLineProxy(element.Child(e=>e.Current.ClassName == "ValueLine") , null);
        }

        public void Accept()
        {
            AcceptButton.ButtonInvoke();
        }

        public AutomationElement AcceptCapture()
        {
            return ValueLine.Element.CaptureWindow(
                  () =>  AcceptButton.ButtonInvoke(),
                  () => "Waiting new windows after click accept button");
        }

        public AutomationElement AcceptButton
        {
            get { return Element.ChildById("btAccept"); }
        }

        public void Cancel()
        {
            CancelButton.ButtonInvoke();
        }

        public AutomationElement CancelButton
        {
            get { return Element.ChildById("btCancel"); }
        }

        public T Value 
        {
            get 
            {
               return  (T)ValueLine.GetValue(typeof(T));
            }

              set 
              {
                  ValueLine.SetValue(value, typeof(T));
            }
        }


    }
 
}
