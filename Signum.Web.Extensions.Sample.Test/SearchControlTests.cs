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
    public class SearchControlTests : Common
    {
        public SearchControlTests()
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

        private Func<bool> thereAreNRows(int n)
        {
            return () => selenium.IsElementPresent("jq=#tblResults > tbody > tr:nth-child(" + n + ")") &&
                         !selenium.IsElementPresent("jq=#tblResults > tbody > tr:nth-child(" + (n + 1) + ")");
        }

        [TestMethod]
        public void Filters()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

                //No filters
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(thereAreNRows(12));

                //Quickfilter of a Lite
                selenium.ContextMenu("jq=#tblResults > tbody > tr:first > td:nth-child(3)");
                selenium.Click("jq=#tblResults > tbody > tr:first > td:nth-child(3) span");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblFilters #trFilter_0"));
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(thereAreNRows(4));

                //Filter from the combo with Subtokens
                selenium.Select("ddlTokens_0", "label=Label");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#lblddlTokens_1"));
                selenium.Click("lblddlTokens_1");
                selenium.Select("ddlTokens_1", "label=Name");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#btnAddFilter"));
                selenium.Click("btnAddFilter");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#value_1"));
                selenium.Type("value_1", "virgin");
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(thereAreNRows(2));

                //Quickfilter of a string
                selenium.ContextMenu("jq=#tblResults > tbody > tr:first > td:nth-child(5)");
                selenium.Click("jq=#tblResults > tbody > tr:first > td:nth-child(5) span");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblFilters #trFilter_2"));
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(thereAreNRows(1));

                //Delete filter
                selenium.Click("btnDelete_2");
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(thereAreNRows(2));

                //Filter from the combo with subtokens of a MList
                selenium.Select("ddlTokens_0", "label=Album");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#lblddlTokens_1"));
                selenium.Click("lblddlTokens_1");
                selenium.Select("ddlTokens_1", "label=Songs");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#lblddlTokens_2"));
                selenium.Click("lblddlTokens_2");
                selenium.Select("ddlTokens_2", "value=Count");
                selenium.Click("btnAddFilter");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#ddlSelector_2"));
                selenium.Select("ddlSelector_2", "value=GreaterThan");
                selenium.Type("value_2", "1");
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(thereAreNRows(1));

                //Delete all filters
                selenium.Click("btnClearAllFilters");
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(thereAreNRows(12));

                //Top
                selenium.Type("sfTop", "5");
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(thereAreNRows(5));
                selenium.Type("sfTop", ""); //Typing "" is as not writing top
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(thereAreNRows(12));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
            }
        }

        private Func<bool> thereAreNRowsInPopup(int n)
        {
            return () => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:nth-child(" + n + ")") &&
                         !selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:nth-child(" + (n + 1) + ")");
        }

        [TestMethod]
        public void FiltersInPopup()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/View/Band/");

                //open search popup
                selenium.Click("Members_btnFind");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_btnSearch"));
                selenium.Click("Members_0_btnSearch");
                selenium.WaitAjaxFinished(thereAreNRowsInPopup(8));

                //Quickfilter of a bool
                selenium.ContextMenu("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(5)");
                selenium.Click("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(5) span");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblFilters #Members_0_trFilter_0"));

                //Quickfilter of an enum
                selenium.ContextMenu("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(6)");
                selenium.Click("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(6) span");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblFilters #Members_0_trFilter_1"));

                selenium.Click("Members_0_btnSearch");
                selenium.WaitAjaxFinished(thereAreNRowsInPopup(7));

                //Quickfilter of an int
                selenium.ContextMenu("jq=#Members_0_tblResults > tbody > tr:nth-child(4) > td:nth-child(3)");
                selenium.Click("jq=#Members_0_tblResults > tbody > tr:nth-child(4) > td:nth-child(3) span");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblFilters #Members_0_trFilter_2"));
                selenium.Select("Members_0_ddlSelector_2", "value=GreaterThan");
                selenium.Click("Members_0_btnSearch");
                selenium.WaitAjaxFinished(thereAreNRowsInPopup(3));

                selenium.Click("Members_0_btnDelete_2");
                selenium.Click("Members_0_btnSearch");
                selenium.WaitAjaxFinished(thereAreNRowsInPopup(7));

                //Filter from the combo with Subtokens
                selenium.Select("Members_0_ddlTokens_0", "label=Artist");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_lblddlTokens_1"));
                selenium.Click("Members_0_lblddlTokens_1");
                selenium.Select("Members_0_ddlTokens_1", "label=Name");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_btnAddFilter"));
                selenium.Click("Members_0_btnAddFilter");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_value_2"));
                selenium.Select("Members_0_ddlSelector_2", "value=EndsWith");
                selenium.Type("Members_0_value_2", "a");
                selenium.Click("Members_0_btnSearch");
                selenium.WaitAjaxFinished(thereAreNRowsInPopup(1));

                //Delete all filters
                selenium.Click("Members_0_btnClearAllFilters");
                selenium.Click("Members_0_btnSearch");
                selenium.WaitAjaxFinished(thereAreNRowsInPopup(8));

                //Top
                selenium.Type("Members_0_sfTop", "5");
                selenium.Click("Members_0_btnSearch");
                selenium.WaitAjaxFinished(thereAreNRowsInPopup(5));
                selenium.Type("Members_0_sfTop", ""); //Typing "" is as not writing top
                selenium.Click("Members_0_btnSearch");
                selenium.WaitAjaxFinished(thereAreNRowsInPopup(8));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
            }
        }

        [TestMethod]
        public void Orders()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

                //Ascending
                selenium.Click("Author");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(3) a[href='View/Artist/5']"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Author.headerSortDown"));

                //Multiple orders
                selenium.ShiftKeyDown();
                selenium.Click("Label");
                selenium.ShiftKeyUp();
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(4) a[href='View/Label/5']"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Author.headerSortDown"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Label.headerSortDown"));

                //Multiple orders: change column order type to descending
                selenium.ShiftKeyDown();
                selenium.Click("Label");
                selenium.ShiftKeyUp();
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(4) a[href='View/Label/3']"));
                Assert.IsTrue(selenium.IsElementPresent("jq=##Author.headerSortDown"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Label.headerSortUp"));

                //Cancel multiple clicking a new order without Shift
                selenium.Click("Label");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(4) a[href='View/Label/7']"));
                Assert.IsFalse(selenium.IsElementPresent("jq=#Author.headerSortDown"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Label.headerSortDown"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
            }
        }

        [TestMethod]
        public void OrdersInPopup()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/View/Band");

                //open search popup
                selenium.Click("Members_btnFind");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_btnSearch"));

                //Ascending
                selenium.Click("Members_0_IsMale");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(5) input:checkbox[value=false]"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Members_0_IsMale.headerSortDown"));

                //Descending
                selenium.Click("Members_0_IsMale");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(5) input:checkbox[value=true]"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Members_0_IsMale.headerSortUp"));

                //Multiple orders
                selenium.ShiftKeyDown();
                selenium.Click("Members_0_Name");
                selenium.ShiftKeyUp();
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(2) a[href='View/Artist/1']"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Members_0_IsMale.headerSortUp"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Members_0_Name.headerSortDown"));

                //Multiple orders: change column order type to descending
                selenium.ShiftKeyDown();
                selenium.Click("Members_0_Name");
                selenium.ShiftKeyUp();
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(2) a[href='View/Artist/8']"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Members_0_IsMale.headerSortUp"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Members_0_Name.headerSortUp"));

                //Cancel multiple clicking a new order without Shift
                selenium.Click("Members_0_Id");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(2) a[href='View/Artist/1']"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#Members_0_Id.headerSortDown"));
                Assert.IsFalse(selenium.IsElementPresent("jq=#Members_0_IsMale.headerSortUp"));
                Assert.IsFalse(selenium.IsElementPresent("jq=#Members_0_Name.headerSortUp"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
            }
        }

        [TestMethod]
        public void UserColumns()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

                //Add 2 user columns
                selenium.Select("ddlTokens_0", "label=Label");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#lblddlTokens_1"));
                selenium.Click("lblddlTokens_1");
                selenium.Select("ddlTokens_1", "label=Id");
                selenium.Click("btnAddColumn");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > thead > tr > th.userColumn[id=Label.Id]"));

                Assert.IsTrue(selenium.IsElementPresent("jq=#btnEditColumns:visible"));

                selenium.Select("ddlTokens_1", "label=Name");
                selenium.Click("btnAddColumn");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > thead > tr > th.userColumn[id=Label.Name]"));

                //Edit names
                selenium.Click("btnEditColumns");
                Assert.IsTrue(selenium.IsElementPresent("jq=#btnEditColumnsFinish:visible"));
                selenium.Type("//input[@value='Label.Id']", "Label Id");
                selenium.Type("//input[@value='Label.Name']", "Label Name");
                selenium.Click("btnEditColumnsFinish");

                Assert.IsTrue(selenium.IsElementPresent("jq=#btnEditColumns:visible"));
                Assert.IsFalse(selenium.IsElementPresent("jq=#btnEditColumnsFinish:visible"));

                //Search with userColumns
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(8)"));

                //Delete one of the usercolumns
                selenium.Click("btnEditColumns");
                selenium.Click("jq=#Label\\.Id > a#link-delete-user-col");
                selenium.Click("btnEditColumnsFinish");
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(8)"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(7)"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
            }
        }

        [TestMethod]
        public void UserColumnsInPopup()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/View/Band");

                //open search popup
                selenium.Click("Members_btnFind");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_btnSearch"));

                //User columns are not present in popup
                Assert.IsFalse(selenium.IsElementPresent("jq=#Members_0_divFilters .addColumn"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
            }
        }
    }
}
