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
using Signum.Utilities;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class Common : SeleniumTestClass
    {
        protected string FindRoute(string webQueryName)
        {
            return "/Signum.Web.Extensions.Sample/Find/{0}".Formato(webQueryName);
        }

        protected string ViewRoute(string webTypeName, int? id)
        {
            return "/Signum.Web.Extensions.Sample/View/{0}/{1}".Formato(webTypeName, id.HasValue ? id.Value.ToString() : "");
        }

        public static void Start()
        {
            Signum.Test.Extensions.Starter.Dirty(); //Force generate database
            Signum.Test.Extensions.Starter.StartAndLoad(UserConnections.Replace(Settings.Default.ConnectionString));

            using (AuthLogic.Disable())
                Schema.Current.Initialize();

            SeleniumTestClass.LaunchSelenium();
        }

        public void Login()
        {
            Login("internal", "internal");
        }

        public void Login(string username, string pwd)
        {
            selenium.Open("/Signum.Web.Extensions.Sample/");

            selenium.WaitAjaxFinished(() => selenium.IsTextPresent("Signum Extensions Sample"));

            //is already logged?
            bool logged = selenium.IsElementPresent("jq=a.sf-logout");
            if (logged)
            {
                selenium.Click("jq=a.sf-logout");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=a.sf-login"));
            }

            selenium.Click("jq=a.sf-login");

            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.Type("username", username);
            selenium.Type("password", pwd);
            selenium.Click("rememberMe");

            selenium.Click("jq=input.login");

            selenium.WaitForPageToLoad(PageLoadTimeout);

            Assert.IsTrue(selenium.IsElementPresent("jq=a.sf-logout"));
        }

        public void LogOut()
        {
            selenium.Click("jq=a.sf-logout");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=a.sf-login"));
        }

        public void CheckLoginAndOpen(string url)
        {
            selenium.Open(url);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            bool logged = selenium.IsElementPresent("jq=a.sf-logout");
            if (!logged)
                Login();

            selenium.Open(url);
            selenium.WaitForPageToLoad(PageLoadTimeout);
        }
    }
}
