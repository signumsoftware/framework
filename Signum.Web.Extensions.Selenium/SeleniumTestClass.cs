using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Selenium;
using System.Diagnostics;

namespace Signum.Web.Selenium
{
    [TestClass]
    public class SeleniumTestClass
    {
        protected static ISelenium selenium;
        protected static Process seleniumServerProcess;

        protected const string PageLoadTimeout = SeleniumExtensions.DefaultPageLoadTimeout; //1.66666667 minutes

        public SeleniumTestClass()
        {

        }

        public static void LaunchSelenium()
        {
            try
            {
                seleniumServerProcess = SeleniumExtensions.LaunchSeleniumProcess();
                selenium = SeleniumExtensions.InitializeSelenium();
            }
            catch (Exception)
            {
                MyTestCleanup();
                throw;
            }
        }

        [ClassCleanup]
        public static void MyTestCleanup()
        {
            try
            {
                selenium.Stop();
                selenium.ShutDownSeleniumServer();
                SeleniumExtensions.KillSelenium(seleniumServerProcess);
            }
            catch (Exception)
            {
                // Ignore errors if unable to close the browser
                throw;
            }
        }
    }
}
