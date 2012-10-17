using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.UIAutomation;
using System.Diagnostics;
using System.Windows.Automation;
using Signum.Engine.Authorization;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Windows.Extensions.Sample.Test.Properties;

namespace Signum.Windows.Extensions.Sample.Test
{
    class Common
    {
        public static MainWindowProxy StartAndLogin()
        {
            return StartAndLogin("su", "su");
        }

        public static MainWindowProxy StartAndLogin(string userName, string password)
        {
            int pid = 0;
            using (WindowProxy login = new WindowProxy(FindOrStartWindow()))
            {
                pid = login.Element.Current.ProcessId;
                login.Element.ChildById("tbUserName").Value(userName);
                login.Element.ChildById("tbPassword").Value(password);

                login.Element.ChildById("btLogin").ButtonInvoke();
            }

            return new MainWindowProxy(AutomationElement.RootElement.WaitChild(a => a.Current.Name == "Music Database" && a.Current.ProcessId == pid, 20 * 1000));
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

        public static void Start()
        {
            //Signum.Test.Extensions.Starter.Dirty(); //Force generate database
            //Signum.Test.Extensions.Starter.StartAndLoad(UserConnections.Replace(Settings.Default.ConnectionString));

            Signum.Test.Extensions.Starter.Start(UserConnections.Replace(Settings.Default.ConnectionString));

            using (AuthLogic.Disable())
                Schema.Current.Initialize();
        }
    }
}
