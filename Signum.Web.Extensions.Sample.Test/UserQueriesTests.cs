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
    public class UserQueriesTests : Common
    {
        public UserQueriesTests()
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
        public void UQ_Create()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

                //add filter
                selenium.Select("ddlTokens_0", "label=Year");
                selenium.Click("btnAddFilter");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("ddlSelector_0"));
                selenium.Select("ddlSelector_0", "label=mayor que");
                selenium.Type("value_0", "2000");

                //add user column
                selenium.Select("ddlTokens_0", "label=Label");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("lblddlTokens_1"));
                selenium.Click("lblddlTokens_1");
                selenium.Select("ddlTokens_1", "label=Owner");
                selenium.Click("btnAddColumn");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=th.userColumn"));

                //add order
                selenium.Click("jq=th#Year");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=th.headerSortDown"));
                selenium.Click("jq=th#Year");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=th.headerSortUp"));
                
                //create user query
                selenium.Click("link=Nuevo");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                selenium.Type("DisplayName", "Last albums");
                selenium.Click("link=Guardar");
                selenium.WaitForPageToLoad(PageLoadTimeout);

                //check new user query is in the dropdownlist
                selenium.Open("/Signum.Web.Extensions.Sample/Find/Album");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                Assert.IsTrue(selenium.IsElementPresent("//a[text()='Last albums']"));
                
                //load user query
                selenium.Click("//a[text()='Last albums']");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                Assert.IsTrue(selenium.IsElementPresent("value_0"));
                Assert.IsTrue(selenium.IsElementPresent("jq=th.userColumn"));
                Assert.IsTrue(selenium.IsElementPresent("jq=th#Year.headerSortUp"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }
    }
}
