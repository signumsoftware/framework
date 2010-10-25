using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Selenium;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Signum.Web.Selenium
{
    public enum WebExplorer
    { 
        IE,
        Chrome,
        Firefox
    }

    public static class SeleniumExtensions
    {
        public static WebExplorer Explorer = WebExplorer.Firefox;

        public static Process LaunchSeleniumProcess()
        {
            Process seleniumServerProcess = new Process();
            seleniumServerProcess.StartInfo.FileName = "java";
            if (Explorer == WebExplorer.Firefox && System.IO.Directory.Exists("D:\\Signum\\Selenium"))
                seleniumServerProcess.StartInfo.Arguments =
                    "-jar c:/selenium/selenium-server.jar -firefoxProfileTemplate D:\\Signum\\Selenium";
            else
                seleniumServerProcess.StartInfo.Arguments =
                    "-jar c:/selenium/selenium-server.jar";
            
            seleniumServerProcess.Start();
            return seleniumServerProcess;
        }

        public static ISelenium InitializeSelenium()
        {
            ISelenium selenium =  new DefaultSelenium("localhost", 
                4444,
                Explorer == WebExplorer.Firefox ? "*chrome" : Explorer == WebExplorer.IE ? "*iexplore" : "*googlechrome", 
                "http://localhost/");

            selenium.Start();
            selenium.SetSpeed("100");
#if (DEBUG)
            selenium.SetSpeed("1000");
#endif
            selenium.SetTimeout("600000");

            selenium.AddLocationStrategy("jq",
            "var loc = locator; " +
            "var attr = null; " +
            "var isattr = false; " +
            "var inx = locator.lastIndexOf('@'); " +

            "if (inx != -1){ " +
            "   loc = locator.substring(0, inx); " +
            "   attr = locator.substring(inx + 1); " +
            "   isattr = true; " +
            "} " +

            "var found = jQuery(inDocument).find(loc); " +
            "if (found.length >= 1) { " +
            "   if (isattr) { " +
            "       return found[0].getAttribute(attr); " +
            "   } else { " +
            "       return found[0]; " +
            "   } " +
            "} else { " +
            "   return null; " +
            "}"
        );

            return selenium;
        }

        public static void KillSelenium(Process seleniumProcess)
        {
            if (seleniumProcess != null && !seleniumProcess.HasExited)
                seleniumProcess.Kill();

            if (System.Environment.MachineName.ToLower().Contains("apolo"))
            {
                //Kill java process so it frees application folder and the next build can delete it
                foreach (var p in Process.GetProcessesByName("java").Where(proc => !proc.HasExited))
                    p.Kill();

                //Kill firefox process so it frees application folder and the next build can delete it
                foreach (var p in Process.GetProcessesByName("firefox").Where(proc => !proc.HasExited))
                    p.Kill();

                //Kill IIS worker process so it frees application folder and the next build can delete it
                //foreach (var p in Process.GetProcessesByName("w3wp").Where(proc => !proc.HasExited))
                //    p.Dispose();
            }
        }

        public const string DefaultPageLoadTimeout = "300000"; // 5 mins // "100000"; //1.66666667 minutes

        public const int DefaultAjaxTimeout = 300000;

        public static void WaitAjaxFinished(this ISelenium selenium, Func<bool> condition)
        {
            WaitAjaxFinished(selenium, condition, DefaultAjaxTimeout);
        }

        public static void WaitAjaxFinished(this ISelenium selenium, Func<bool> condition, int timeout)
        {
            DateTime limit = DateTime.Now.AddMilliseconds(timeout);
            Debug.WriteLine(timeout);
            Debug.WriteLine(condition());
            while (DateTime.Now < limit && !condition())
            {
                Debug.WriteLine(DateTime.Now < limit);
                Debug.WriteLine(condition());
                Thread.Sleep(1000);
            }
            Assert.IsTrue(condition());
        }
    }
}
