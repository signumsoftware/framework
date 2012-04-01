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
                login.Owner.ChildById("tbUserName").Pattern<ValuePattern>().SetValue(userName);
                login.Owner.ChildById("tbPassword").Pattern<ValuePattern>().SetValue(password);

                login.Owner.ChildById("btLogin").Pattern<InvokePattern>().Invoke();
            }

            return new WindowProxy(AutomationElement.RootElement.WaitChild(10000, a => a.Current.Name == "Music Database"));
        }

        private static AutomationElement FindOrStartWindow()
        {
            var win = AutomationElement.RootElement.TryChild(a => a.Current.Name == "Login on Music Database");

            if (win != null)
                return win;

            Process p = Process.Start(@"D:\Signum\Extensions\Signum.Windows.Extensions.Sample\bin\Debug\Signum.Windows.Extensions.Sample.exe");

            p.WaitForInputIdle();

            return AutomationElement.RootElement.WaitChild(5000, a => a.Current.ProcessId == p.Id);
        }
    }
}
