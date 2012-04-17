using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;

namespace Signum.Windows.UIAutomation
{
    public class WindowProxy : IDisposable
    {
        public AutomationElement Element { get; private set; }

        private WindowPattern wp;

        public WindowProxy(AutomationElement element)
        {
            this.Element = element;
            wp = element.Pattern<WindowPattern>();
        }

        public virtual void Dispose()
        {
            Close();
        }

        public void Wait(int milliseconds)
        {
            wp.WaitForInputIdle(milliseconds);
        }

        public virtual bool Close()
        {
            try
            {
                if (Element.Current.IsOffscreen)
                    return false;

                wp.Close();

                return true;
            }
            catch (ElementNotAvailableException ena)
            {
                if (ena.Message.Contains("The target element corresponds to UI that is no longer available (for example, the parent window has closed)."))
                    return false;

                throw ena;
            }
        }

        public MessageBoxProxy TryGetCurrentMessageBox()
        {
            var win = Element.TryChild(a => a.Current.ControlType == ControlType.Window && a.Current.ClassName == "#32770");

            if (win != null)
                return new MessageBoxProxy(win);

            return null;
        }

        public MessageBoxProxy WaitCurrentMessageBox()
        {
            var win = Element.WaitChild(a => a.Current.ControlType == ControlType.Window && a.Current.ClassName == "#32770");

            return new MessageBoxProxy(win);
        }
    }


    public class MessageBoxProxy : WindowProxy
    {
        public MessageBoxProxy(AutomationElement element): base(element)
        {
        }

        public AutomationElement OkButton
        {
            get { return Element.TryChildById("1") ?? Element.ChildById("2"); }
        }

        public AutomationElement CancelButton
        {
            get { return Element.ChildById("2"); }//Warning!!
        }

        public AutomationElement YesButton
        {
            get { return Element.ChildById("6"); }
        }

        public AutomationElement NoButton
        {
            get { return Element.ChildById("7"); }
        }
    }
}
