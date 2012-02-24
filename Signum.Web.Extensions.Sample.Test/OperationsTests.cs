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
using Signum.Web.Operations;
using System.Threading;

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
            //Album.Save
            CheckLoginAndOpen(FindRoute("Album"));

            selenium.SearchCreate();
            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.Type("Name", "test");
            selenium.Type("Year", "2010");
            selenium.LineFindWithImplAndSelectElements("Author_", "Band", false, new int[]{0});
            selenium.Select("Label", "label=Virgin");

            selenium.EntityButtonClick("AlbumOperation_Save");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void Operations002_Execute_ReloadContent()
        {
            //Album.Modify
            CheckLoginAndOpen(ViewRoute("Album", 1));

            Execute_AlbumOperationModify(selenium, "Siamese Dreamm");

            //Restore state for future tests
            Execute_AlbumOperationModify(selenium, "Siamese Dream");
        }

        void Execute_AlbumOperationModify(ISelenium selenium, string name)
        {
            selenium.Type("Name", name);
            selenium.EntityButtonClick("AlbumOperation_Modify");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("//span[contains(@class,'sf-entity-title') and text()='{0}']".Formato(name)));
        }

        [TestMethod]
        public void Operations003_ConstructFrom()
        {
            //Album.Clone
            CheckLoginAndOpen(ViewRoute("Album", 1));

            Assert.IsFalse(selenium.EntityButtonEnabled("AlbumOperation_Save"));

            selenium.EntityMenuOptionClick(constructorsMenuId, "AlbumOperation_Clone");
            selenium.WaitAjaxFinished(() => selenium.EntityButtonEnabled("AlbumOperation_Save"));

            selenium.Type("Name", "test3");
            selenium.Type("Year", "2010");

            selenium.EntityButtonClick("AlbumOperation_Save");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void Operations004_ConstructFrom_OpenPopup()
        {
            //Album.CreateFromBand
            CheckLoginAndOpen(ViewRoute("Band", 1));

            selenium.EntityMenuOptionClick(constructorsMenuId, "AlbumOperation_CreateFromBand");
            string popupPrefix = "New_";
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(popupPrefix)));

            selenium.Type("{0}Name".Formato(popupPrefix), "test2");
            selenium.Type("{0}Year".Formato(popupPrefix), "2010");
            selenium.LineFindAndSelectElements("{0}Label_".Formato(popupPrefix), false, new int[] { 0 });

            //When clicking Ok (Save) => Custom controller that returns a url => navigate
            selenium.PopupSave(popupPrefix);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void Operations005_ConstructFrom_OpenPopupAndSubmitFormAndPopup()
        {
            //Album.Clone
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
            //Album.Delete
            CheckLoginAndOpen(ViewRoute("Album", 13));

            Assert.IsTrue(selenium.IsElementPresent("AlbumOperation_Delete"));
            selenium.EntityButtonClick("AlbumOperation_Delete");

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));

            selenium.WaitForPageToLoad(PageLoadTimeout);

            //Delete has redirected to search window => Check deleted album doesn't exist any more
            selenium.Search();
            Assert.IsFalse(selenium.IsElementPresent(SearchTestExtensions.EntityRowSelector("Album;13")));
        }

        [TestMethod]
        public void Operations007_ConstructFromMany()
        {
            //Album.CreateGreatestHits
            CheckLoginAndOpen("{0}?allowMultiple=true".Formato(FindRoute("Album")));

            selenium.Search();

            selenium.SelectRowCheckbox(0);
            selenium.SelectRowCheckbox(1);

            selenium.QueryMenuOptionClick(constructorsMenuId, "AlbumOperation_CreateEmptyGreatestHitsAlbum");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.EntityButtonLocator("AlbumOperation_Save")));

            selenium.Type("New_Name", "test greatest empty");
            selenium.Select("New_Label", "label=Virgin");

            selenium.EntityButtonClick("AlbumOperation_Save");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.EntityButtonLocator("AlbumOperation_Modify")));
        }

        [TestMethod]
        public void Operations008_ConstructFromMany_Submit()
        {
            //Album.CreateGreatestHits
            CheckLoginAndOpen("{0}?allowMultiple=true".Formato(FindRoute("Album")));

            selenium.Search();
            
            selenium.SelectRowCheckbox(0);
            selenium.SelectRowCheckbox(1);

            selenium.QueryMenuOptionClick(constructorsMenuId, "AlbumOperation_CreateGreatestHitsAlbum");
            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.Type("Name", "test greatest hits");
            selenium.Select("Label", "label=Virgin");

            selenium.EntityButtonClick("AlbumOperation_Save");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }
    }
}
