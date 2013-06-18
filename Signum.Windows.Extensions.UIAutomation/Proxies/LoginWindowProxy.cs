using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
    public class LoginWindowProxy : WindowProxy
    {
        public LoginWindowProxy(AutomationElement element)
            : base(element)
        {
            element.AssertClassName("Login", allowHwndWrapper: true);
        }

        AutomationElement LoginCapture(string userName, string password, int? timeOut = null)
        {
            Element.ChildById("tbUserName").Value(userName);
            Element.ChildById("tbPassword").Value(password);

            var pid = Element.Current.ProcessId;
            var previous = WaitExtensions.GetAllProcessWindows(pid).Select(a => a.GetRuntimeId().ToString(".")).ToHashSet();

            Element.ChildById("btLogin").ButtonInvoke();

            var error = Element.ChildById("txtError");

            AutomationElement newWindow = null;

            Element.Wait(() =>
            {
                newWindow = WaitExtensions.GetAllProcessWindows(pid).FirstOrDefault(a => !previous.Contains(a.GetRuntimeId().ToString(".")));

                MessageBoxProxy.AssertNoErrorWindow(newWindow);

                if (newWindow != null)
                    return true;

                if (!error.Current.IsOffscreen && error.Current.Name.HasText())
                    throw new MessageBoxErrorException("LoginWindow: " + error.Current.Name);

                return false;
            }, () => "Waiting for login", timeOut ?? WaitExtensions.CapturaWindowTimeout);

            return newWindow;
        }

        public static AutomationElement LoginAndContinue(AutomationElement loginWindow, string userName, string password, int? timeOut = null)
        {
            using (LoginWindowProxy login = new LoginWindowProxy(loginWindow))
            {
                return login.LoginCapture(userName, password, timeOut);
            }
        }

    }
}
