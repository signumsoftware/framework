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

        public static Process LaunchSeleniumProcess()
        {
            Process seleniumServerProcess = new Process();
            seleniumServerProcess.StartInfo.FileName = "java";
            if (Explorer == WebExplorer.Firefox && System.IO.Directory.Exists("D:\\Selenium"))
                seleniumServerProcess.StartInfo.Arguments =
                    "-jar c:/selenium/selenium-server.jar -firefoxProfileTemplate D:\\Selenium -timeout 3600";
            else
                seleniumServerProcess.StartInfo.Arguments =
                    "-jar c:/selenium/selenium-server.jar -log selenium.log -timeout 3600";

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

            selenium.SetTimeout("600000");
            selenium.SetSpeed("200");
            //selenium.SetSpeed("1000");



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
            bool starting = true;
            int attempts = 0;
            while (starting)
            {
                try
                {
                    selenium.Start();
                    starting = false;
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

            //if (System.Environment.MachineName.ToLower().Contains("apolo"))
            //{
            //    //Kill java process so it frees application folder and the next build can delete it
            //    foreach (var p in Process.GetProcessesByName("java").Where(proc => !proc.HasExited))
            //        p.Kill();

            //    //Kill firefox process so it frees application folder and the next build can delete it
            //    foreach (var p in Process.GetProcessesByName("firefox").Where(proc => !proc.HasExited))
            //        p.Kill();

            //    //Kill IIS worker process so it frees application folder and the next build can delete it
            //    //foreach (var p in Process.GetProcessesByName("w3wp").Where(proc => !proc.HasExited))
            //    //    p.Dispose();
            //}
        }

        public const string DefaultPageLoadTimeout = "20000";
        public const string PageLoadLongTimeout = "40000";

        //public const int DefaultAjaxTimeout = 10000;
        public static int DefaultAjaxTimeout = 30000;
        public static int DefaultAjaxWait = 200;


        public static void WaitAjaxFinished(this ISelenium selenium, Expression<Func<bool>> condition)
        {
            WaitAjaxFinished(selenium, condition, DefaultAjaxTimeout);
        }

        public static void WaitAjaxFinished(this ISelenium selenium, Expression<Func<bool>> condition, int? timeout = null)
        {
            var func = condition.Compile();
            if (func())
                return;

            DateTime limit = DateTime.Now.AddMilliseconds(timeout ?? DefaultAjaxTimeout);
            do
            {
                Thread.Sleep(DefaultAjaxWait);

                if (func())
                    return;

            } while (DateTime.Now < limit);


            throw new TimeoutException("The expression took more than {0} ms:\r\n{1}".Formato(timeout ?? DefaultAjaxTimeout, condition.NiceToString()));
        }

        public static string PopupSelector(string prefix)
        {
            return "jq=#{0}Temp:visible".Formato(prefix);
        }

        public static void PopupCancel(this ISelenium selenium, string prefix)
        {
            selenium.Click("jq=.ui-dialog-titlebar-close:visible");
            Assert.IsFalse(selenium.IsElementPresent("{0}:visible".Formato(PopupSelector(prefix))));
        }

        public static void PopupCancelDiscardChanges(this ISelenium selenium, string prefix)
        {
            selenium.Click("jq=.ui-dialog-titlebar-close:visible");
            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            Assert.IsFalse(selenium.IsElementPresent("{0}:visible".Formato(PopupSelector(prefix))));
        }

        public static void PopupOk(this ISelenium selenium, string prefix)
        {
            PopupOk(selenium, prefix, false);
        }

        public static void PopupOk(this ISelenium selenium, string prefix, bool submit)
        {
            selenium.Click("jq=#{0}btnOk".Formato(prefix));
            if (submit)
                selenium.WaitForPageToLoad(DefaultPageLoadTimeout);
            else
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent(PopupSelector(prefix)));
        }

        public static void PopupSave(this ISelenium selenium, string prefix)
        {
            EntityButtonClick(selenium, prefix + "ebSave");
        }

        public static void MainEntityHasId(this ISelenium selenium)
        {
            Assert.IsTrue(selenium.IsElementPresent("jq=#divNormalControl[data-isnew=false]"));
        }

        public static bool PopupEntityHasUnsavedChanges(this ISelenium selenium, string prefix)
        {
            return selenium.IsElementPresent("jq=#{0}divMainControl.sf-changed".Formato(prefix));
        }

        public static string RuntimeInfoSelector(string prefix)
        {
            return RuntimeInfoSelector(prefix, -1);
        }

        public static string RuntimeInfoSelector(string prefix, int elementIndexBase0)
        {
            if (elementIndexBase0 != -1)
                prefix += elementIndexBase0 + "_";

            return "jq=#{0}sfRuntimeInfo".Formato(prefix);
        }

        public static string EntityButtonLocator(string buttonId)
        {
            //check of css class is redundant but it must be in the html, so good for testing
            return "jq=#{0}.sf-entity-button".Formato(buttonId);
        }

        public static string EntityMenuOptionLocator(string menuId, string optionId)
        {
            //check of menu and item classes is redundant but it must be in the html, so good for testing
            return "jq=#{0}.sf-dropdown ul.sf-menu-button li.ui-menu-item a.sf-entity-button#{1}".Formato(menuId, optionId);
        }

        public static void EntityButtonSaveClick(this ISelenium selenium)
        {
            EntityButtonClick(selenium, "ebSave");
        }

        public static void EntityButtonSaveClick(this ISelenium selenium, string prefix)
        {
            EntityButtonClick(selenium, prefix + "ebSave");
        }

        public static bool EntityOperationEnabled(this ISelenium selenium, Enum operationKey)
        {
            return selenium.EntityButtonEnabled(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static bool EntityButtonEnabled(this ISelenium selenium, string idButton)
        {
            return selenium.IsElementPresent("{0}:not(.sf-disabled)".Formato(EntityButtonLocator(idButton)));
        }

        public static bool EntityOperationDisabled(this ISelenium selenium, Enum operationKey)
        {
            return selenium.EntityButtonDisabled(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static bool EntityButtonDisabled(this ISelenium selenium, string idButton)
        {
            return selenium.IsElementPresent("{0}.sf-disabled".Formato(EntityButtonLocator(idButton)));
        }

        public static void EntityOperationClick(this ISelenium selenium, Enum operationKey)
        {
            selenium.EntityButtonClick(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static void EntityButtonClick(this ISelenium selenium, string idButton)
        {
            selenium.Click("{0}:not(.sf-disabled)".Formato(EntityButtonLocator(idButton)));
        }

        public static void EntityMenuConstructFromClick(this ISelenium selenium, Enum constructFromKey)
        {
            selenium.EntityMenuOptionClick("tmConstructors", constructFromKey.GetType().Name + "_" + constructFromKey.ToString());
        }

        public static void EntityMenuOptionClick(this ISelenium selenium, string menuId, string optionId)
        {
            selenium.Click(EntityMenuOptionLocator(menuId, optionId));
        }

        public static bool EntityMenuConstructFromEnabled(this ISelenium selenium, Enum constructFromKey)
        {
            return selenium.EntityMenuOptionEnabled("tmConstructors", constructFromKey.GetType().Name + "_" + constructFromKey.ToString());
        }

        public static bool EntityMenuOptionEnabled(this ISelenium selenium, string menuId, string optionId)
        {
            string locator = EntityMenuOptionLocator(menuId, optionId);
            Assert.IsTrue(selenium.IsElementPresent(locator));
            return !selenium.IsElementPresent("{0}.sf-disabled".Formato(locator));
        }

        public static string ValidationSummarySelector(string prefix)
        {
            return "jq=#{0}sfGlobalValidationSummary".Formato(prefix);
        }

        public static bool FormHasNErrors(this ISelenium selenium, int? numberOfErrors)
        {
            return FormHasNErrors(selenium, numberOfErrors, "");
        }

        public static bool FormHasNErrors(this ISelenium selenium, int? numberOfErrors, string prefix)
        {
            if (numberOfErrors.HasValue)
            {
                return selenium.IsElementPresent("{0} > ul > li:nth-child({1})".Formato(ValidationSummarySelector(prefix), numberOfErrors)) &&
                       !selenium.IsElementPresent("{0} > ul > li:nth-child({1})".Formato(ValidationSummarySelector(prefix), numberOfErrors + 1));
            }
            else
            {
                return selenium.IsElementPresent("{0} > ul > li".Formato(ValidationSummarySelector(prefix)));
            }
        }

        public static bool FormElementHasError(this ISelenium selenium, string elementId)
        {
            return selenium.IsElementPresent("jq=#{0}.input-validation-error".Formato(elementId));
        }
    }
}
