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
using Signum.Entities.Reports;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class ControlPanelTests : Common
    {
        public ControlPanelTests()
        {
            using (AuthLogic.UnsafeUserSession("su"))
            {
                object queryName = typeof(AlbumDN);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                
                UserQueryDN userQuery = new UserQueryDN(queryName)
                {
                    Related = Database.Query<RoleDN>().Where(r=>r.Name == "InternalUser").Select(a=>a.ToLite<IdentifiableEntity>()).Single(),
                    DisplayName = "test",
                    Filters= 
                    {
                        new QueryFilterDN("Id", 3) { Operation = FilterOperation.GreaterThan }
                    },
                }.ParseAndSave();
            }
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Common.Start();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Common.MyTestCleanup();
        }

        [TestMethod]
        public void ControlPanel001_Create()
        {
            CheckLoginAndOpen(FindRoute("ControlPanel"));

            selenium.SearchCreate();
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //Related is RoleDN.Current, and when available UserDN.Current
            selenium.Type("DisplayName", "Control Panel Home Page");
            selenium.Click("HomePage");
            selenium.Type("NumberOfColumns", "2");

            selenium.EntityButtonSaveClick();
            selenium.WaitForPageToLoad(PageLoadTimeout);

            string partsPrefix = "Parts_";

            //SearchControlPart with userquery created in UQ_Create test
            CreateNewPart("UserQueryPart");
            string part0 = partsPrefix + "0_";
            selenium.Type(part0 + "Title", "Last Albums");
            selenium.LineFindAndSelectElements(part0 + "Content_UserQuery_", false, new int[]{ 0 });

            //CountSearchControlPart with userquery created in UQ_Create test
            CreateNewPart("CountSearchControlPart");
            string part1 = partsPrefix + "1_";
            selenium.Type(part1 + "Title", "My Count Controls");
            string part1ContentUQsPrefix = part1 + "Content_UserQueries_";
            selenium.LineCreate(part1ContentUQsPrefix, false, 0);
            selenium.RepeaterWaitUntilItemLoaded(part1ContentUQsPrefix, 0);
            selenium.LineFindAndSelectElements(part1ContentUQsPrefix + "0_UserQuery_", false, new int[] { 0 });
            
            //LinkListPart - drag to second column
            CreateNewPart("LinkListPart");
            string part2 = partsPrefix + "2_";
            selenium.Type(part2 + "Title", "My Links");
            CreateLinkListPartItem(selenium, part2, 0, "Best Band", "http://localhost/Signum.Web.Extensions.Sample/View/Band/1");
            CreateLinkListPartItem(selenium, part2, 1, "Best Artist", "http://localhost/Signum.Web.Extensions.Sample/View/Artist/1");
            
            //selenium.MouseDown("jq=#sfCpAdminContainer td[data-column=1] .sf-ftbl-part:eq(2)");
            //selenium.MouseMove("jq=#sfCpAdminContainer td[data-column=2] .sf-ftbl-droppable");
            //selenium.MouseUp("jq=#sfCpAdminContainer td[data-column=2] .sf-ftbl-droppable");

            selenium.DragAndDropToObject("jq=#sfCpAdminContainer td[data-column=1] .sf-ftbl-part:eq(2)",
                "jq=#sfCpAdminContainer td[data-column=2] .sf-ftbl-droppable");

            selenium.EntityButtonSaveClick();
            selenium.WaitForPageToLoad(PageLoadTimeout);

            ////view
            //selenium.Open("/Signum.Web.Extensions.Sample/");
            //selenium.WaitForPageToLoad(PageLoadTimeout);
            //Assert.IsTrue(selenium.IsElementPresent("{0} #r1c1_divSearchControl".Formato(PartFrontEndSelector(1, 1))));
            //Assert.IsTrue(selenium.IsElementPresent("{0} #lblr2c1 + a.count-search".Formato(PartFrontEndSelector(2, 1))));
            //Assert.IsTrue(selenium.IsElementPresent("{0} li:nth-child(2) > a".Formato(PartFrontEndSelector(2, 2))));
        }

        void CreateNewPart(string partType)
        {
            selenium.EntityButtonClick("CreatePart");

            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector("New_")));
            selenium.Click(partType);
            selenium.WaitForPageToLoad(PageLoadTimeout);
        }

        string PartFrontEndSelector(int rowIndexBase1, int colIndexBase1)
        {
            return "jq=table > tbody > tr:nth-child({0}) > td:nth-child({1})".Formato(rowIndexBase1, colIndexBase1);
        }

        void CreateLinkListPartItem(ISelenium selenium, string partPrefix, int linkIndexBase0, string label, string link)
        {
            string partContentLinksPrefix = partPrefix + "Content_Links_";

            selenium.LineCreate(partContentLinksPrefix, false, 0);
            selenium.RepeaterWaitUntilItemLoaded(partContentLinksPrefix, 0);
            string partContentLinksItemPrefix = partContentLinksPrefix + linkIndexBase0 + "_";
            selenium.Type(partContentLinksItemPrefix + "Label", label);
            selenium.Type(partContentLinksItemPrefix + "Link", link);
        }
    }
}
