using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Web.Selenium;
using System.Text.RegularExpressions;
using Signum.Utilities;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class OperationsCtxMenuTests : Common
    {
        public OperationsCtxMenuTests()
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
        public void OperationCtxMenu001_Execute()
        {
            CheckLoginAndOpen(FindRoute("Artist"));

            //Search
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 1)));

            string row1col1 = SearchTestExtensions.CellSelector(selenium, 1, 1);

            //ArtistOperations.AssignPersonalAward
            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, "ArtistOperation_AssignPersonalAward");

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0} > a.sf-entity-ctxmenu-success".Formato(row1col1)));
            Assert.IsFalse(selenium.IsElementPresent("{0} .sf-search-ctxmenu .sf-search-ctxmenu-overlay".Formato(row1col1)));

            //For Michael Jackson there are no operations enabled
            selenium.EntityContextMenu(5);
            //There's not a menu with hrefs => only some text saying there are no operations
            Assert.IsFalse(selenium.IsElementPresent("{0} a".Formato(SearchTestExtensions.EntityContextMenuSelector(selenium, 5))));
        }

        [TestMethod]
        public void OperationCtxMenu002_ConstructFrom_OpenPopup()
        {
            CheckLoginAndOpen(FindRoute("Band"));

            //Search
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 1)));

            string row1col1 = SearchTestExtensions.CellSelector(selenium, 1, 1);

            //Band.CreateFromBand
            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, "AlbumOperation_CreateFromBand");

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector("New_")));

            selenium.Type("New_Name", "ctxtest");
            selenium.Type("New_Year", DateTime.Now.Year.ToString());

            selenium.LineFindAndSelectElements("New_Label_", false, new int[] { 0 });

            selenium.Click("jq=#{0}btnOk".Formato("New_")); //Dont't call PopupOk helper => it makes an ajaxWait and then waitPageLoad fails
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void OperationCtxMenu003_ConstructFrom_Navigate()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            //Search
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 1)));

            //Album.Clone
            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, "AlbumOperation_Clone");

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#AlbumOperation_Save"));

            selenium.Type("Name", "ctxtest2");
            selenium.Type("Year", DateTime.Now.Year.ToString());

            selenium.Click("AlbumOperation_Save");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void OperationCtxMenu004_Delete()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            //Album.Delete
            //Order by Id descending so we delete the last cloned album
            int idCol = 2;
            selenium.Sort(idCol, true);
            selenium.Sort(idCol, false);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 1)));

            Assert.IsTrue(selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 14)));
            Assert.IsFalse(selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 15)));

            string row1col1 = SearchTestExtensions.CellSelector(selenium, 1, 1);

            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, "AlbumOperation_Delete");

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.Search();
            
            Assert.IsTrue(selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 13)));
            Assert.IsFalse(selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 14)));
        }
    }
}
