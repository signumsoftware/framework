using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
    public class WindowProxy : IDisposableException
    {
        public AutomationElement Element { get; private set; }

        private WindowPattern wp;

        public WindowProxy(AutomationElement element)
        {
            this.Element = element;
            wp = element.Pattern<WindowPattern>();
        }

        //http://roslyn.codeplex.com/discussions/551007
        public void OnException(Exception exception)
        {
            this.CurrentException = exception;
        }

        public Exception CurrentException { get; set; }

        public event Action Disposed;

        public virtual void Dispose()
        {
            try
            {
                Close();
                OnDisposed();
            }
            catch
            {
                if (CurrentException == null)
                    throw;
            }
        }

        protected void OnDisposed()
        {
            Disposed?.Invoke();
        }

        public bool WaitForInputIdle(int? timeOut = null)
        {
            return wp.WaitForInputIdle(timeOut ?? WaitExtensions.DefaultTimeout);
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
            catch (ElementNotAvailableException)
            {
                return false;
            }
        }

        public static AutomationElement Normalize(AutomationElement element)
        {
            if (element.Current.ControlType == ControlType.Window)
                return element;

            TreeWalker walker = new TreeWalker(ConditionBuilder.ToCondition(a => a.Current.ControlType == ControlType.Window));

            return walker.Normalize(element);
        }
    }
}
