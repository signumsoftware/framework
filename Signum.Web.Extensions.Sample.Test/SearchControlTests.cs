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
                throw;
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
                throw;
            }
        }

        [TestMethod]
        public void Orders()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

                //Ascending
                string authorth = "jq=#tblResults > thead > tr > th:nth-child(3)";
                selenium.Click(authorth); //Author
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(3) a[href='View/Artist/5']"));
                Assert.IsTrue(selenium.IsElementPresent(authorth + ".headerSortDown"));

                //Multiple orders
                string labelth = "jq=#tblResults > thead > tr > th:nth-child(4)";
                selenium.ShiftKeyDown();
                selenium.Click(labelth); //Label
                selenium.ShiftKeyUp();
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(4) a[href='View/Label/5']"));
                Assert.IsTrue(selenium.IsElementPresent(authorth + ".headerSortDown"));
                Assert.IsTrue(selenium.IsElementPresent(labelth + ".headerSortDown"));

                //Multiple orders: change column order type to descending
                selenium.ShiftKeyDown();
                selenium.Click(labelth); //Label
                selenium.ShiftKeyUp();
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(4) a[href='View/Label/3']"));
                Assert.IsTrue(selenium.IsElementPresent(authorth + ".headerSortDown"));
                Assert.IsTrue(selenium.IsElementPresent(labelth + ".headerSortUp"));

                //Cancel multiple clicking a new order without Shift
                selenium.Click(labelth); //Label
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(4) a[href='View/Label/7']"));
                Assert.IsFalse(selenium.IsElementPresent(authorth + ".headerSortDown"));
                Assert.IsTrue(selenium.IsElementPresent(labelth + ".headerSortDown"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
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
                string ismaleth = "jq=#Members_0_tblResults > thead > tr > th:nth-child(5)";
                selenium.Click(ismaleth);
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(5) input:checkbox[value=false]"));
                Assert.IsTrue(selenium.IsElementPresent(ismaleth + ".headerSortDown"));

                //Descending
                selenium.Click(ismaleth); 
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(5) input:checkbox[value=true]"));
                Assert.IsTrue(selenium.IsElementPresent(ismaleth + ".headerSortUp"));

                //Multiple orders
                string nameth = "jq=#Members_0_tblResults > thead > tr > th:nth-child(4)";
                selenium.ShiftKeyDown();
                selenium.Click(nameth); 
                selenium.ShiftKeyUp();
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(2) a[href='View/Artist/1']"));
                Assert.IsTrue(selenium.IsElementPresent(ismaleth + ".headerSortUp"));
                Assert.IsTrue(selenium.IsElementPresent(nameth + ".headerSortDown"));

                //Multiple orders: change column order type to descending
                selenium.ShiftKeyDown();
                selenium.Click(nameth); 
                selenium.ShiftKeyUp();
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(2) a[href='View/Artist/8']"));
                Assert.IsTrue(selenium.IsElementPresent(ismaleth + ".headerSortUp"));
                Assert.IsTrue(selenium.IsElementPresent(nameth + ".headerSortUp"));

                //Cancel multiple clicking a new order without Shift
                string idth = "jq=#Members_0_tblResults > thead > tr > th:nth-child(3)";
                selenium.Click(idth);
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Members_0_tblResults > tbody > tr:first > td:nth-child(2) a[href='View/Artist/1']"));
                Assert.IsTrue(selenium.IsElementPresent(idth + ".headerSortDown"));
                Assert.IsFalse(selenium.IsElementPresent(ismaleth + ".headerSortUp"));
                Assert.IsFalse(selenium.IsElementPresent(nameth + ".headerSortUp"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
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
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > thead > tr > th > :hidden[value=Label.Id]"));

                Assert.IsTrue(selenium.IsElementPresent("jq=#btnEditColumns:visible"));

                selenium.Select("ddlTokens_1", "label=Name");
                selenium.Click("btnAddColumn");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > thead > tr > th > :hidden[value=Label.Name]"));

                //Edit names
                selenium.Click("btnEditColumns");
                Assert.IsTrue(selenium.IsElementPresent("jq=#btnEditColumnsFinish:visible"));
                selenium.Type("jq=#tblResults > thead > tr > th:nth-child(7) > :text", "Label Id");
                selenium.Type("jq=#tblResults > thead > tr > th:nth-child(8) > :text", "Label Name");
                selenium.Click("btnEditColumnsFinish");

                Assert.IsTrue(selenium.IsElementPresent("jq=#btnEditColumns:visible"));
                Assert.IsFalse(selenium.IsElementPresent("jq=#btnEditColumnsFinish:visible"));

                //Search with userColumns
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(8)"));

                //Delete one of the usercolumns
                selenium.Click("btnEditColumns");
                selenium.Click("jq=#tblResults > thead > tr > th:nth-child(7) > a#link-delete-user-col");
                selenium.Click("btnEditColumnsFinish");
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(8)"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:nth-child(7)"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
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
                throw;
            }
        }

        [TestMethod]
        public void EntityCtxMenu_OpExecute()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Artist");

                //Search
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr"));

                //ArtistOperations.AssignPersonalAward
                selenium.ContextMenu("jq=#tblResults > tbody > tr:first > td:first");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:first .searchCtxMenu a"));
                selenium.Click("jq=#tblResults > tbody > tr:first > td:first .searchCtxMenu a");
                
                Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:first > a.entityCtxMenuSuccess"));
                Assert.IsFalse(selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:first .searchCtxMenu .searchCtxMenuOverlay"));

                //For Michael Jackson there are no operations enabled
                selenium.ContextMenu("jq=#tblResults > tbody > tr:nth-child(5) > td:first");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:nth-child(5) > td:first .searchCtxMenu"));
                //There's not a menu with hrefs => only some text saying there are no operations
                Assert.IsFalse(selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:first .searchCtxMenu a"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }

        [TestMethod]
        public void EntityCtxMenu_OpConstructFrom_OpenPopup()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Band");

                //Search
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr"));

                //Band.CreateFromBand
                selenium.ContextMenu("jq=#tblResults > tbody > tr:first > td:first");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:first .searchCtxMenu a"));
                selenium.Click("jq=#tblResults > tbody > tr:first > td:first .searchCtxMenu a");
                
                Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #NewTemp"));

                selenium.Type("New_Name", "ctxtest");
                selenium.Type("New_Year", DateTime.Now.Year.ToString());
                selenium.Click("New_Label_btnFind");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #New_LabelTemp"));
                selenium.Click("New_Label_btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#New_LabelTemp .tblResults > tbody > tr"));
                selenium.Click("New_Label_rowSelection");
                selenium.Click("New_Label_sfBtnOk");
                
                selenium.Click("New_sfBtnOk");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                Assert.IsTrue(selenium.IsElementPresent("jq=.entityId > span"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }

        [TestMethod]
        public void EntityCtxMenu_OpConstructFrom_Navigate()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

                //Search
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr"));

                //Album.Clone
                selenium.ContextMenu("jq=#tblResults > tbody > tr:first > td:first");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:first .operation-ctx-menu"));
                selenium.Click("jq=#tblResults > tbody > tr:first > td:first .operation-ctx-menu li:nth-child(2) > a");

                Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#AlbumOperation_Save"));

                selenium.Type("Name", "ctxtest2");
                selenium.Type("Year", DateTime.Now.Year.ToString());
                
                selenium.Click("AlbumOperation_Save");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                Assert.IsTrue(selenium.IsElementPresent("jq=.entityId > span"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }

        [TestMethod]
        public void EntityCtxMenu_OpDelete()
        {
            try
            {
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

                //Album.Delete
                //Order by Id descending so we delete the last cloned album
                selenium.Click("jq=th#Id");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr"));
                selenium.Click("jq=th#Id");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr"));

                Assert.IsTrue(selenium.IsElementPresent("jq=#tblResults > tbody > tr:nth-child(14)"));
                Assert.IsFalse(selenium.IsElementPresent("jq=#tblResults > tbody > tr:nth-child(15)"));

                selenium.ContextMenu("jq=#tblResults > tbody > tr:first > td:first");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr:first > td:first .operation-ctx-menu"));
                selenium.Click("jq=#tblResults > tbody > tr:first > td:first .operation-ctx-menu li:first > a");

                Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
                selenium.WaitForPageToLoad(PageLoadTimeout);

                Assert.IsTrue(selenium.IsElementPresent("jq=#tblResults > tbody > tr:nth-child(13)"));
                Assert.IsFalse(selenium.IsElementPresent("jq=#tblResults > tbody > tr:nth-child(14)"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }
    }
}
