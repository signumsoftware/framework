using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.UIAutomation;
using System.Diagnostics;
using System.Windows.Automation;

namespace Signum.Windows.Extensions.Sample.Test
{
    class Common
    {
        public static WindowProxy StartAndLogin()
        {
            return StartAndLogin("su", "su");
        }

        public static WindowProxy StartAndLogin(string userName, string password)
        {
            using (WindowProxy login = new WindowProxy(FindOrStartWindow()))
            {
                login.Element.ChildById("tbUserName").Value(userName);
                login.Element.ChildById("tbPassword").Value(password);

                login.Element.ChildById("btLogin").ButtonInvoke();
            }

            return new WindowProxy(AutomationElement.RootElement.WaitChild(a => a.Current.Name == "Music Database", 10000));
        }

        private static AutomationElement FindOrStartWindow()
        {
            var win = AutomationElement.RootElement.TryChild(a => a.Current.Name == "Login on Music Database");

            if (win != null)
                return win;

            Process p = Process.Start(@"D:\Signum\Extensions\Signum.Windows.Extensions.Sample\bin\Debug\Signum.Windows.Extensions.Sample.exe");

            p.WaitForInputIdle();

            return AutomationElement.RootElement.WaitChild(a => a.Current.ProcessId == p.Id, 5000);
        }
    }
}
