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
using System.Threading;
using Signum.Test.Extensions;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class OperationsTests : Common
    {
        public OperationsTests()
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

        string constructorsMenuId = "tmConstructors";

        [TestMethod]
        public void Operations001_Execute_Navigate()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            selenium.SearchCreate();
            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.Type("Name", "test");
            selenium.Type("Year", "2010");
            selenium.LineFindWithImplAndSelectElements("Author_", "Band", false, new int[]{0});
            selenium.Select("Label_sfCombo", "label=Virgin");

            selenium.EntityOperationClick(AlbumOperation.Save);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void Operations002_Execute_ReloadContent()
        {
            CheckLoginAndOpen(ViewRoute("Album", 1));

            string name = "Siamese Dreamm";

            selenium.Type("Name", name);
            selenium.EntityOperationClick(AlbumOperation.Modify);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("//span[contains(@class,'sf-entity-title') and text()='{0}']".Formato(name)));
        }

        [TestMethod]
        public void Operations003_ConstructFrom()
        {
            CheckLoginAndOpen(ViewRoute("Album", 1));

            Assert.IsFalse(selenium.EntityOperationEnabled(AlbumOperation.Save));

            selenium.EntityMenuConstructFromClick(AlbumOperation.Clone);
            selenium.WaitAjaxFinished(() => selenium.EntityOperationEnabled(AlbumOperation.Save));

            selenium.Type("Name", "test3");
            selenium.Type("Year", "2010");

            selenium.EntityOperationClick(AlbumOperation.Save);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void Operations004_ConstructFrom_OpenPopup()
        {
            CheckLoginAndOpen(ViewRoute("Band", 1));

            selenium.EntityMenuConstructFromClick(AlbumOperation.CreateFromBand);
            string popupPrefix = "New_";
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(popupPrefix)));

            selenium.Type("{0}Name".Formato(popupPrefix), "test2");
            selenium.Type("{0}Year".Formato(popupPrefix), "2010");
            selenium.LineFindAndSelectElements("{0}Label_".Formato(popupPrefix), false, new int[] { 0 });

            selenium.Click("{0}btnOk".Formato(popupPrefix));
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void Operations005_ConstructFrom_OpenPopupAndSubmitFormAndPopup()
        {
            CheckLoginAndOpen(ViewRoute("Album", 1));

            selenium.EntityButtonClick("CloneWithData");

            string popupPrefix = "New_";
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:visible".Formato(SeleniumExtensions.PopupSelector(popupPrefix))));

            selenium.Type("{0}StringValue".Formato(popupPrefix), "test popup");
            selenium.PopupOk(popupPrefix);

            selenium.IsTextPresent("test popup");
        }

        [TestMethod]
        public void Operations006_Delete()
        {
            CheckLoginAndOpen(ViewRoute("Album", 13));

            selenium.EntityOperationClick(AlbumOperation.Delete);
            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //Delete has redirected to search window => Check deleted album doesn't exist any more
            selenium.Search();
            Assert.IsFalse(selenium.IsElementPresent(SearchTestExtensions.EntityRowSelector("Album;13")));
        }

        [TestMethod]
        public void Operations007_ConstructFromMany()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            selenium.Search();

            selenium.SelectRowCheckbox(0);
            selenium.SelectRowCheckbox(1);

            selenium.Click("jq=.sf-tm-selected");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("AlbumOperation_CreateEmptyGreatestHitsAlbum"));
            selenium.Click("AlbumOperation_CreateEmptyGreatestHitsAlbum");

            selenium.WaitAjaxFinished(() => selenium.EntityOperationEnabled(AlbumOperation.Save));

            selenium.Type("New_Name", "test greatest empty");
            selenium.Select("New_Label_sfCombo", "label=Virgin");

            selenium.EntityOperationClick(AlbumOperation.Save);
            selenium.WaitAjaxFinished(() => selenium.EntityOperationEnabled(AlbumOperation.Modify));
        }
    }
}
