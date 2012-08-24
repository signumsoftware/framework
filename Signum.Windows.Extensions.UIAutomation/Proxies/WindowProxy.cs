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

        public void AssertMessageBoxError()
        {
              var mb = TryGetCurrentMessageBox();
            try
            {
                if (mb != null && mb.IsError)
                    throw new MessageBoxErrorException("Error MessageBox shown: {0}\r\nMessage: {1}".Formato(mb.Title, mb.Message));
            }
            finally
            {
                if (mb != null)
                    mb.Close();
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



        public static int WindowAfterTimeout = 5 * 1000;

        public AutomationElement GetWindowAfter(Action action, Func<string> actionDescription = null, int? timeOut = null)
        {
            var previous = AutomationElement.RootElement.Children(a => a.Current.ProcessId == Element.Current.ProcessId).Select(a => a.GetRuntimeId().ToString(".")).ToHashSet();

            action();

            AutomationElement newWindow = null;

            Element.Wait(() =>
            {
                newWindow = AutomationElement.RootElement
                    .Children(a => a.Current.ProcessId == Element.Current.ProcessId)
                    .FirstOrDefault(a => !previous.Contains(a.GetRuntimeId().ToString(".")));

                if (newWindow != null)
                    return true;

                AssertMessageBoxError();

                return false;
            }, actionDescription, timeOut ?? WindowAfterTimeout);

            return newWindow;
        }

        public AutomationElement GetModalWindowAfter(Action action, Func<string> actionDescription, int? timeOut = null)
        {
            TreeWalker walker = new TreeWalker(ConditionBuilder.ToCondition(a => a.Current.ControlType == ControlType.Window));

            var parentWindow = walker.Normalize(Element);

            action();

            AutomationElement newWindow = null;

            Element.Wait(() =>
            {
                newWindow = walker.GetFirstChild(parentWindow);


                if (newWindow != null)
                    return true;

                AssertMessageBoxError();

                return false;
            }, actionDescription, timeOut ?? WindowAfterTimeout);
            return newWindow;
        }



        
    }

    [Serializable]
    public class MessageBoxErrorException : Exception
    {
        public MessageBoxErrorException() { }
        public MessageBoxErrorException(string message) : base(message) { }
        public MessageBoxErrorException(string message, Exception inner) : base(message, inner) { }
        protected MessageBoxErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }


    public class MessageBoxProxy : WindowProxy
    {
        public static Func<string, string, bool> ContainsErrorMessage = (title, message) =>
            title.Contains("error", StringComparison.InvariantCultureIgnoreCase) ||
            message.Contains("error", StringComparison.InvariantCultureIgnoreCase) ||
            title.Contains("exception", StringComparison.InvariantCultureIgnoreCase) ||
            message.Contains("exception", StringComparison.InvariantCultureIgnoreCase);

        public MessageBoxProxy(AutomationElement element)
            : base(element)
        {
        }

        public string Title
        {
            get { return Element.Current.Name; }
        }

        public string Message
        {
            get { return Element.ChildById("65535").Current.Name; }
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

        public bool IsError
        {
            get { return ContainsErrorMessage(Title, Message); }
        }
    }
}
