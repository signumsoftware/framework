using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;

namespace Signum.Windows.UIAutomation
{
    public class WindowProxy : IDisposable
    {
        public AutomationElement Owner { get; private set; }

        private WindowPattern wp;

        public WindowProxy(AutomationElement automationElement)
        {
            this.Owner = automationElement;
            wp = automationElement.Pattern<WindowPattern>();
        }

        public void Dispose()
        {
            Close();
        }

        public void Wait(int milliseconds)
        {
            wp.WaitForInputIdle(milliseconds);
        }

        private bool Close()
        {
            try
            {
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
    }
}
