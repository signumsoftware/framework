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
    public class ControlPanelTests : Common
    {
        public ControlPanelTests()
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
        public void ControlPanel_Create()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/ControlPanel");

                selenium.Click("jq=input.create");
                selenium.WaitForPageToLoad(PageLoadTimeout);

                //Related is RoleDN.Current, and when available UserDN.Current
                selenium.Type("DisplayName", "Control Panel Home Page");
                selenium.Click("HomePage");
                selenium.Type("NumberOfColumns", "2");
                
                //SearchControlPart with userquery created in UQ_Create test
                selenium.Click("Parts_btnCreate");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Parts_0_sfRepeaterItem"));
                selenium.Type("Parts_0_Title", "Last Albums");
                selenium.Click("Parts_0_Fill");
                selenium.Click("Parts_0_Content_btnFind");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #Parts_0_ContentTemp"));
                selenium.Click("UserQuery");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Parts_0_Content_btnSearch"));
                selenium.Click("jq=#Parts_0_Content_btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Parts_0_Content_tblResults > tbody > tr"));
                selenium.Click("jq=#Parts_0_Content_rowSelection");
                selenium.Click("jq=#Parts_0_Content_sfBtnOk");
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("jq=#divASustituir + #Parts_0_ContentTemp"));
                
                //CountSearchControlPart with userquery created in UQ_Create test
                selenium.Click("Parts_btnCreate");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Parts_1_sfRepeaterItem"));
                selenium.Type("Parts_1_Title", "My Count Controls");
                selenium.Type("Parts_1_Row", "2");
                selenium.Type("Parts_1_Column", "1");
                selenium.Click("Parts_1_Content_btnCreate");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #Parts_1_ContentTemp"));
                selenium.Click("CountSearchControlPart");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Parts_1_Content_UserQueries_btnCreate"));
                selenium.Click("Parts_1_Content_UserQueries_btnCreate");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("Parts_1_Content_UserQueries_0_Label"));
                selenium.Type("Parts_1_Content_UserQueries_0_Label", "Last Albums Count");
                selenium.Click("Parts_1_Content_UserQueries_0_UserQuery_btnFind");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #Parts_1_Content_UserQueries_0_UserQueryTemp"));
                selenium.Click("jq=#Parts_1_Content_UserQueries_0_UserQuery_btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Parts_1_Content_UserQueries_0_UserQuery_tblResults > tbody > tr"));
                selenium.Click("jq=#Parts_1_Content_UserQueries_0_UserQuery_rowSelection");
                selenium.Click("jq=#Parts_1_Content_UserQueries_0_UserQuery_sfBtnOk");
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("jq=#divASustituir + #Parts_1_Content_UserQueries_0_UserQuery_ContentTemp"));
                selenium.Click("jq=#Parts_1_Content_sfBtnOk");
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("jq=#divASustituir + #Parts_1_ContentTemp"));

                //LinkListPart
                selenium.Click("Parts_btnCreate");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Parts_2_sfRepeaterItem"));
                selenium.Type("Parts_2_Title", "My Links");
                selenium.Type("Parts_2_Row", "2");
                selenium.Type("Parts_2_Column", "2");
                selenium.Click("Parts_2_Content_btnCreate");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #Parts_2_ContentTemp"));
                selenium.Click("LinkListPart");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Parts_2_Content_Links_btnCreate"));
                selenium.Click("Parts_2_Content_Links_btnCreate");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("Parts_2_Content_Links_0_Label"));
                selenium.Type("Parts_2_Content_Links_0_Label", "Best Band");
                selenium.Type("Parts_2_Content_Links_0_Link", "http://localhost/Signum.Web.Extensions.Sample/View/Band/1");
                selenium.Click("Parts_2_Content_Links_btnCreate");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("Parts_2_Content_Links_1_Label"));
                selenium.Type("Parts_2_Content_Links_1_Label", "Best Artist");
                selenium.Type("Parts_2_Content_Links_1_Link", "http://localhost/Signum.Web.Extensions.Sample/View/Artist/1");
                selenium.Click("jq=#Parts_2_Content_sfBtnOk");
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("jq=#divASustituir + #Parts_2_ContentTemp"));

                selenium.Click("btnSave");
                selenium.WaitForPageToLoad(PageLoadTimeout);

                //view
                selenium.Open("/Signum.Web.Extensions.Sample/");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                Assert.IsTrue(selenium.IsElementPresent("jq=table > tbody > tr:first > td:first #r1c1_divSearchControl"));
                Assert.IsTrue(selenium.IsElementPresent("jq=table > tbody > tr:nth-child(2) > td:first #lblr2c1 + a.count-search"));
                Assert.IsTrue(selenium.IsElementPresent("jq=table > tbody > tr:nth-child(2) > td:nth-child(2) li:nth-child(2) > a"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }
    }
}
