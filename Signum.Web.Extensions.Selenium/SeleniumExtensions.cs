using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Selenium;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Utilities;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

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
        public static readonly string DefaultTimeout = "180000";

        public static Process LaunchSeleniumProcess()
        {
            Process seleniumServerProcess = new Process();
            seleniumServerProcess.StartInfo.FileName = "java";
            if (Explorer == WebExplorer.Firefox && System.IO.Directory.Exists("D:\\Selenium"))
                seleniumServerProcess.StartInfo.Arguments =
                    "-jar c:/selenium/selenium-server.jar -firefoxProfileTemplate D:\\Selenium -timeout 180";
            else
                seleniumServerProcess.StartInfo.Arguments =
                    "-jar c:/selenium/selenium-server.jar -log selenium.log -timeout 180"; /*timeout in seconds*/

            seleniumServerProcess.Start();
            return seleniumServerProcess;
        }

        public static ISelenium InitializeSelenium()
        {
            ISelenium selenium = new DefaultSelenium("localhost",
                4444,
                Explorer == WebExplorer.Firefox ? "*chrome" : Explorer == WebExplorer.IE ? "*iexplore" : "*googlechrome",
                "http://localhost/");

            StartSelenium(selenium);

            selenium.SetTimeout(DefaultTimeout); //timeout in ms => 3mins
            //selenium.SetSpeed("200");
            
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


        private static void StartSelenium(ISelenium selenium)
        {
            int attempts = 0;
            while (true)
            {
                try
                {
                    selenium.Start();
                    return;
                }
                catch (Exception)
                {
                    attempts += 1;
                    System.Threading.Thread.Sleep(3000);
                    if (attempts > 8)
                        throw new ApplicationException("Could not start selenium");
                }
            }
        }

        public static void KillSelenium(Process seleniumProcess)
        {
            if (seleniumProcess != null && !seleniumProcess.HasExited)
                seleniumProcess.Kill();
        }

        public static string PageLoadTimeout = "20000";
        public static int AjaxTimeout = 20000;
        public static int AjaxWait = 200;

        public static void WaitForPageToLoad(this ISelenium selenium)
        {
            selenium.WaitForPageToLoad(PageLoadTimeout);
        }

        public static void Wait(this ISelenium selenium, Func<bool> condition, Func<string> actionDescription = null, int? timeout = null)
        {
            if (condition())
                return;

            DateTime limit = DateTime.Now.AddMilliseconds(timeout ?? AjaxTimeout);
            do
            {
                Thread.Sleep(AjaxWait);

                if (condition())
                    return;

            } while (DateTime.Now < limit);


            throw new TimeoutException("Timeout after {0} ms waiting for {1} in page {2}({3})".Formato(
                timeout ?? AjaxTimeout,
                actionDescription == null ? "visual condition" : actionDescription(),
                selenium.GetTitle(),
                selenium.GetLocation()));
        }

        public static void WaitElementPresent(this ISelenium selenium, string locator, Func<string> actionDescription = null, int? timeout = null)
        {
            selenium.Wait(() => selenium.IsElementPresent(locator), actionDescription ?? (Func<string>)(() => "{0} to be present".Formato(locator)), timeout);
        }

        public static void WaitElementDisapear(this ISelenium selenium, string locator, Func<string> actionDescription = null, int? timeout = null)
        {
            selenium.Wait(() => !selenium.IsElementPresent(locator), actionDescription ?? (Func<string>)(() => "{0} to disapear".Formato(locator)), timeout);
        }

        public static void AssertElementPresent(this ISelenium selenium, string locator)
        {
            if (!selenium.IsElementPresent(locator))
                throw new InvalidOperationException("{0} not found".Formato(locator));
        }

        public static void AssertElementNotPresent(this ISelenium selenium, string locator)
        {
            if (selenium.IsElementPresent(locator))
                throw new InvalidOperationException("{0} is found".Formato(locator));
        }

        public static void SetChecked(this ISelenium selenium, string locator, bool isChecked)
        {
            if (selenium.IsChecked(locator) == isChecked)
                return;

            if (isChecked)
                selenium.Check(locator);
            else
                selenium.Uncheck(locator);
        }

        public static void ConsumeConfirmation(this ISelenium selenium)
        {
            selenium.Wait(() => selenium.IsConfirmationPresent(), () => "confirmation present");
            selenium.GetConfirmation();
        }


        public static void ConsumeAlert(this ISelenium selenium)
        {
            selenium.Wait(() => selenium.IsAlertPresent(), () => "alert present");
            selenium.GetAlert();
        }
     
    }
}
