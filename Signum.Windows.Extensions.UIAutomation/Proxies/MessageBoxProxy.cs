using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
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

        public static bool IsMessageBox(AutomationElement windowElement)
        {
            return windowElement.Current.ControlType == ControlType.Window && windowElement.Current.ClassName == "#32770";
        }

        public static void AssertErrorWindow(AutomationElement windowElement)
        {
            if (windowElement != null && IsMessageBox(windowElement))
            {
                var mb = new MessageBoxProxy(windowElement);

                mb.AssertError();
            }
        }

        public void AssertError()
        {
            try
            {
                if (IsError)
                    throw new MessageBoxErrorException("Error MessageBox shown: {0}\r\nMessage: {1}".Formato(Title, Message));
            }
            finally
            {
                this.Close();
            }
        }
    }

    public static class MessageBoxExtensions
    {
        public static void AssertMessageBoxChild(this AutomationElement element)
        {
            var mb = element.TryMessageBoxChild();

            if (mb != null)
                mb.AssertError();
        }

        public static MessageBoxProxy TryMessageBoxChild(this AutomationElement element)
        {
            var parentWin = WindowProxy.Normalize(element);

            var win = parentWin.TryChild(a => a.Current.ControlType == ControlType.Window && a.Current.ClassName == "#32770");

            if (win != null)
                return new MessageBoxProxy(win);

            return null;
        }

        public static MessageBoxProxy WaitMessageBoxChild(this AutomationElement element)
        {
            var parentWin = WindowProxy.Normalize(element);

            var win = parentWin.WaitChild(a => a.Current.ControlType == ControlType.Window && a.Current.ClassName == "#32770");

            return new MessageBoxProxy(win);
        }
    }
}
