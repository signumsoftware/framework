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

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class ReportsTests : Common
    {
        public ReportsTests()
            : base()
        {

        }

        [ClassInitialize()]
        public static void LaunchSelenium(TestContext testContext)
        {
            Common.LaunchSelenium(testContext);
        }

        [ClassCleanup]
        public static void MyTestCleanup()
        {
            Common.MyTestCleanup();
        }

        [TestMethod]
        public void ExcelReport()
        {
            CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

            //create when there's no query created => direct navigation to create page
            selenium.Click("link=Administrar");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.Type("DisplayName", "prueba");
            selenium.Type("File", "D:\\Signum\\Pruebas\\Albumchulo.xlsx");
            selenium.Click("link=Guardar");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            Assert.IsTrue(selenium.IsElementPresent("jq=.entityId span"));

            //modify
            selenium.Type("DisplayName", "prueba 2");
            selenium.Click("link=Guardar");
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //created appears modified in menu
            selenium.Click("link=Albums");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            Assert.IsTrue(selenium.IsElementPresent("link=prueba 2"));
            
            //delete
            selenium.Click("link=Administrar");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=a[href=View/ExcelReport/1]"));
            selenium.Click("jq=a[href=View/ExcelReport/1]");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.Click("link=Eliminar");
            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), "^¿Está seguro de eliminar el informe prueba 2[\\s\\S]$"));
            selenium.WaitForPageToLoad(PageLoadTimeout);
            
            //deleted does not appear in menu
            selenium.Click("link=Albums");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            Assert.IsFalse(selenium.IsElementPresent("link=prueba 2"));

            //create when there are already others
            selenium.Click("link=Administrar");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.Click("jq=input.create");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.Type("DisplayName", "prueba 3");
            selenium.Type("File", "D:\\Signum\\Pruebas\\Albumchulo.xlsx");
            selenium.Click("link=Guardar");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            
            //created appears modified in menu
            selenium.Click("link=Albums");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            Assert.IsTrue(selenium.IsElementPresent("link=prueba 3"));
        }
    }
}
