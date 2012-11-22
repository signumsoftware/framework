using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Web.Selenium;
using System.Text.RegularExpressions;
using Signum.Utilities;
using Signum.Entities.Processes;
using Signum.Engine.Authorization;
using Signum.Engine;
using Signum.Test;
using Signum.Test.Extensions;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class OperationsCtxTests : Common
    {
        public OperationsCtxTests()
        {

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
        public void OperationCtx001_Execute()
        {
            CheckLoginAndOpen(FindRoute("Artist"));

            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 1)));

            string row1col1 = SearchTestExtensions.CellSelector(selenium, 1, 1);

            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, "ArtistOperation_AssignPersonalAward");

            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0} > a.sf-entity-ctxmenu-success".Formato(row1col1)));
            Assert.IsFalse(selenium.IsElementPresent("{0} .sf-search-ctxmenu .sf-search-ctxmenu-overlay".Formato(row1col1)));

            //For Michael Jackson there are no operations enabled
            selenium.EntityContextMenu(5);
            //There's not a menu with hrefs => only some text saying there are no operations
            Assert.IsFalse(selenium.IsElementPresent("{0} a".Formato(SearchTestExtensions.EntityContextMenuSelector(selenium, 5))));
        }

        [TestMethod]
        public void OperationCtx002_ConstructFrom_OpenPopup()
        {
            CheckLoginAndOpen(FindRoute("Band"));

            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 1)));

            string row1col1 = SearchTestExtensions.CellSelector(selenium, 1, 1);

            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, "AlbumOperation_CreateFromBand");

            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector("New_")));

            selenium.Type("New_Name", "ctxtest");
            selenium.Type("New_Year", DateTime.Now.Year.ToString());

            selenium.LineFindAndSelectElements("New_Label_", false, new int[] { 0 });

            selenium.Click("New_btnOk");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void OperationCtx003_Delete()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            int cuantos = 0;
            using (AuthLogic.Disable())
            {
                cuantos = Database.Query<AlbumDN>().Count();
            }

            //Order by Id descending so we delete the last cloned album
            int idCol = 3;
            selenium.Sort(idCol, true);
            selenium.Sort(idCol, false);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 1)));

            selenium.WaitAjaxFinished(selenium.ThereAreNRows(cuantos));

            string row1col1 = SearchTestExtensions.CellSelector(selenium, 1, 1);

            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, "AlbumOperation_Delete");

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.Search();

            selenium.WaitAjaxFinished(selenium.ThereAreNRows(cuantos - 1));
        }

        [TestMethod]
        public void OperationCtx004_ConstructFromMany()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            selenium.Search();

            selenium.SelectRowCheckbox(0);
            selenium.SelectRowCheckbox(1);

            selenium.EntityContextMenu(2);
            selenium.EntityContextMenuClick(2, "AlbumOperation_CreateGreatestHitsAlbum");
            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.Type("Name", "test greatest hits");
            selenium.Select("Label_sfCombo", "label=Virgin");

            selenium.EntityOperationClick(AlbumOperation.Save);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void OperationCtx010_FromMany_Execute()
        {
            CheckLoginAndOpen(FindRoute("Artist"));

            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 1)));

            selenium.SelectRowCheckbox(1);
            selenium.SelectRowCheckbox(2);

            selenium.EntityContextMenu(2);
            selenium.EntityContextMenuClick(2, "ArtistOperation_AssignPersonalAward");

            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector("New_")));
            selenium.EntityOperationClick(ProcessOperation.Execute);

            selenium.WaitAjaxFinished(() => 
                !selenium.EntityOperationEnabled(ProcessOperation.Execute) &&
                !selenium.EntityOperationEnabled(ProcessOperation.Suspend));

            selenium.PopupCancel("New_");
        }

        [TestMethod]
        public void OperationCtx011_FromMany_Delete()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            int cuantos = 0;
            using (AuthLogic.Disable())
            {
                cuantos = Database.Query<AlbumDN>().Count();
            }

            //Order by Id descending so we delete the last cloned album
            int idCol = 3;
            selenium.Sort(idCol, true);
            selenium.Sort(idCol, false);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 1)));

            selenium.WaitAjaxFinished(selenium.ThereAreNRows(cuantos));

            string row1col1 = SearchTestExtensions.CellSelector(selenium, 1, 1);
            selenium.SelectRowCheckbox(1);
            selenium.SelectRowCheckbox(2);

            selenium.Click("jq=.sf-tm-selected");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("AlbumOperation_Delete"));
            selenium.Click("AlbumOperation_Delete");

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));

            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector("New_")));
            selenium.EntityOperationClick(ProcessOperation.Execute);

            selenium.WaitAjaxFinished(() =>
                !selenium.EntityOperationEnabled(ProcessOperation.Execute) &&
                !selenium.EntityOperationEnabled(ProcessOperation.Suspend));

            selenium.PopupCancel("New_");

            selenium.Search();

            selenium.WaitAjaxFinished(selenium.ThereAreNRows(cuantos - 2));
        }
    }
}
