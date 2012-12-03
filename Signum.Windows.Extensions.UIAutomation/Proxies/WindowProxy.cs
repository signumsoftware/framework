using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using Signum.Utilities;

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

        public event Action Disposed;

        public virtual void Dispose()
        {
            Close();
            OnDisposed();
        }

        protected void OnDisposed()
        {
            if (Disposed != null)
                Disposed();
        }

        public bool WaitForInputIdle(int? timeOut = null)
        {
            return wp.WaitForInputIdle(timeOut ?? WaitExtensions.DefaultTimeOut);
        }


        public virtual bool IsClosed
        {
            get
            {
                try
                {
                    return Element.Current.IsOffscreen;
                }
                catch (ElementNotAvailableException)
                {
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
                catch (COMException)
                {
                    return true;
                }
            }
        }

        public virtual bool Close()
        {
            try
            {
                if (IsClosed)
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

       

        public static AutomationElement Normalize(AutomationElement element)
        {
            TreeWalker walker = new TreeWalker(ConditionBuilder.ToCondition(a => a.Current.ControlType == ControlType.Window));

            return walker.Normalize(element);
        }
    }

    public static class WindowExtensions
    {
        public static T Next<F, T>(this F from, Func<F, T> wizarAction)
            where F : WindowProxy
            where T : WindowProxy
        {
            using (from)
            {
                return wizarAction(from);
            }
        }

        public static void End<F>(this F from, Action<F> wizarAction)
            where F : WindowProxy
        {
            using (from)
            {
                wizarAction(from);
            }
        }
    }
}
