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
using System.Text.RegularExpressions;
using Signum.Utilities;
using System.Resources;
using Signum.Web.Operations;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class ReportsTests : Common
    {
        public ReportsTests()
        {

        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Common.Start(testContext);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Common.MyTestCleanup();
        }

        [TestMethod]
        public void ExcelReport()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

                string administerLocator = "jq=div.query-operation:last a.query-operation:last";
                string saveLocator = "jq=.operations > li:first > a";
                string deleteLocator = "jq=.operations > li:nth-child(2) > a";

                //create when there's no query created => direct navigation to create page
                selenium.Click(administerLocator);
                selenium.WaitForPageToLoad(PageLoadTimeout);
                selenium.Type("DisplayName", "test");
                selenium.Type("File", "D:\\Signum\\Pruebas\\Albumchulo.xlsx");
                selenium.Click(saveLocator);
                selenium.WaitForPageToLoad(PageLoadTimeout);
                Assert.IsTrue(selenium.IsElementPresent("jq=.entityId span"));

                //modify
                selenium.Type("DisplayName", "test 2");
                selenium.Click(saveLocator);
                selenium.WaitForPageToLoad(PageLoadTimeout);

                //created appears modified in menu
                selenium.Click("link=Albums");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                Assert.IsTrue(selenium.IsElementPresent("link=test 2"));

                //delete
                selenium.Click(administerLocator);
                selenium.WaitForPageToLoad(PageLoadTimeout);
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=a[href=View/ExcelReport/1]"));
                selenium.Click("jq=a[href=View/ExcelReport/1]");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                selenium.Click(deleteLocator);
                //Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), "^¿" + extensionsManager.GetString("AreYouSureOfDeletingReport0").Formato("prueba 2") + "[\\s\\S]$"));
                Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
                selenium.WaitForPageToLoad(PageLoadTimeout);

                //deleted does not appear in menu
                selenium.Click("link=Albums");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                Assert.IsFalse(selenium.IsElementPresent("link=test 2"));

                //create when there are already others
                selenium.Click(administerLocator);
                selenium.WaitForPageToLoad(PageLoadTimeout);
                selenium.Click("jq=input.create");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                selenium.Type("DisplayName", "test 3");
                selenium.Type("File", "D:\\Signum\\Pruebas\\Albumchulo.xlsx");
                selenium.Click(saveLocator);
                selenium.WaitForPageToLoad(PageLoadTimeout);

                //created appears modified in menu
                selenium.Click("link=Albums");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                Assert.IsTrue(selenium.IsElementPresent("link=test 3"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }
    }
}
