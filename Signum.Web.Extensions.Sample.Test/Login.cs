using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Web.Selenium;
using Selenium;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Authorization;
using Signum.Test;
using Signum.Web.Extensions.Sample.Test.Properties;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class Common
    {
        protected static ISelenium selenium;
        protected static Process seleniumServerProcess;

        protected const string PageLoadTimeout = SeleniumExtensions.DefaultPageLoadTimeout; //1.66666667 minutes

        public Common()
        {

        }

        [ClassInitialize()]
        public static void LaunchSelenium(TestContext testContext)
        {
            seleniumServerProcess = SeleniumExtensions.LaunchSeleniumProcess();

            Signum.Test.Extensions.Starter.Start(Settings.Default.ConnectionString);
            
            using (AuthLogic.Disable())
                Schema.Current.Initialize();
            
            selenium = SeleniumExtensions.InitializeSelenium();
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
        }

        [TestCleanup]
        public void StopSelenium()
        {
            //selenium.Stop();
        }

        [ClassCleanup]
        public static void MyTestCleanup()
        {
            try
            {
                selenium.Stop();
                SeleniumExtensions.KillSelenium(seleniumServerProcess);
            }
            catch (Exception)
            {
                // Ignore errors if unable to close the browser
            }
        }

        [TestMethod]
        public void Login()
        {
            Login("internal", "internal");
        }

        private void Login(string username, string pwd)
        {
            selenium.Open("/Signum.Web.Extensions.Sample/");

            selenium.WaitAjaxFinished(() => selenium.IsTextPresent("Signum Extensions Sample"));

            //is already logged?
            bool logged = selenium.IsElementPresent("jq=a.logout");
            if (logged)
            {
                selenium.Click("jq=a.logout");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=a.login"));
            }

            selenium.Click("jq=a.login");

            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.Type("username", username);
            selenium.Type("password", pwd);
            selenium.Click("rememberMe");

            selenium.Click("jq=input.login");

            selenium.WaitForPageToLoad(PageLoadTimeout);

            Assert.IsTrue(selenium.IsElementPresent("jq=a.logout"));
        }

        [TestMethod]
        public void LogOut()
        {
            selenium.Click("jq=a.logout");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=a.login"));
        }

        public void CheckLoginAndOpen(string url)
        {
            selenium.Open(url);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            bool logged = selenium.IsElementPresent("jq=a.logout");
            if (!logged)
                Login();

            selenium.Open(url);
            selenium.WaitForPageToLoad(PageLoadTimeout);
        }
    }
}
