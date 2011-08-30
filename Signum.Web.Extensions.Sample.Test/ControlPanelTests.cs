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
using Signum.Web.UserQueries;
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
            using (AuthLogic.Disable())
            {
                object queryName = typeof(AlbumDN);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                
                UserQueryDN userQuery = new UserQueryDN(queryName)
                {
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
            Common.Start(testContext);
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
                
            string partsPrefix = "Parts_";

            //SearchControlPart with userquery created in UQ_Create test
            string part0 = "{0}0_".Formato(partsPrefix);
            selenium.LineCreate(partsPrefix, false, 0);
            selenium.RepeaterWaitUntilItemLoaded(partsPrefix, 0);
            SetPartBasicProperties(selenium, part0, 1, 1, true, "Last Albums");
            selenium.LineFindWithImplAndSelectElements("{0}Content_".Formato(part0), "UserQuery", false, new int[]{0});
                
            //CountSearchControlPart with userquery created in UQ_Create test
            string part1 = "{0}1_".Formato(partsPrefix);
            selenium.LineCreate(partsPrefix, false, 1);
            selenium.RepeaterWaitUntilItemLoaded(partsPrefix, 1);
            SetPartBasicProperties(selenium, part1, 2, 1, false, "My Count Controls");
            string part1content = "{0}Content_".Formato(part1);
            selenium.LineCreateWithImpl(part1content, true, "CountSearchControlPart");
            string part1contentUserQueries = "{0}UserQueries_".Formato(part1content);
            selenium.LineCreate(part1contentUserQueries, false);
            selenium.RepeaterWaitUntilItemLoaded(part1contentUserQueries, 0);
            selenium.Type("{0}0_Label".Formato(part1contentUserQueries), "Last Albums Count");

            selenium.LineFindAndSelectElements("{0}0_UserQuery_".Formato(part1contentUserQueries), false, new int[] { 0 });
            
            selenium.PopupOk(part1content);

            //LinkListPart
            string part2 = "{0}2_".Formato(partsPrefix);
            selenium.LineCreate(partsPrefix, false, 2);
            selenium.RepeaterWaitUntilItemLoaded(partsPrefix, 2);
            SetPartBasicProperties(selenium, part2, 2, 2, false, "My Links");
            string part2content = "{0}Content_".Formato(part2);
            selenium.LineCreateWithImpl(part2content, true, "LinkListPart");
            string part2contentLinks = "{0}Links_".Formato(part2content);
            CreateLinkListPart(selenium, part2contentLinks, 0, "Best Band", "http://localhost/Signum.Web.Extensions.Sample/View/Band/1");
            CreateLinkListPart(selenium, part2contentLinks, 1, "Best Artist", "http://localhost/Signum.Web.Extensions.Sample/View/Artist/1");
            selenium.PopupOk(part2content);

            selenium.EntityButtonSaveClick();
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //view
            selenium.Open("/Signum.Web.Extensions.Sample/");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            Assert.IsTrue(selenium.IsElementPresent("{0} #r1c1_divSearchControl".Formato(PartFrontEndSelector(1, 1))));
            Assert.IsTrue(selenium.IsElementPresent("{0} #lblr2c1 + a.count-search".Formato(PartFrontEndSelector(2, 1))));
            Assert.IsTrue(selenium.IsElementPresent("{0} li:nth-child(2) > a".Formato(PartFrontEndSelector(2, 2))));
        }

        string PartFrontEndSelector(int rowIndexBase1, int colIndexBase1)
        {
            return "jq=table > tbody > tr:nth-child({0}) > td:nth-child({1})".Formato(rowIndexBase1, colIndexBase1);
        }

        void CreateLinkListPart(ISelenium selenium, string partPrefix, int linkIndexBase0, string label, string link)
        {
            selenium.LineCreate(partPrefix, false, linkIndexBase0);
            selenium.RepeaterWaitUntilItemLoaded(partPrefix, linkIndexBase0);
            selenium.Type("{0}{1}_Label".Formato(partPrefix, linkIndexBase0), label);
            selenium.Type("{0}{1}_Link".Formato(partPrefix, linkIndexBase0), link);
        }

        void SetPartBasicProperties(ISelenium selenium, string partPrefix, int rowIndexBase1, int colIndexBase1, bool fillRow, string partTitle)
        {
            selenium.Type("{0}Row".Formato(partPrefix), rowIndexBase1.ToString());
            selenium.Type("{0}Column".Formato(partPrefix), colIndexBase1.ToString());
            if (fillRow)
                selenium.Click("{0}Fill".Formato(partPrefix));
            selenium.Type("{0}Title".Formato(partPrefix), partTitle);
        }
    }
}
