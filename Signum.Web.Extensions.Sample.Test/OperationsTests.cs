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
            Common.Start(testContext);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Common.MyTestCleanup();
        }

        string constructorsMenuId = "tmConstructors";

        [TestMethod]
        public void Execute_Navigate()
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
        public void Execute_ReloadContent()
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
        public void ConstructFrom_OpenPopup()
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
            selenium.Click("jq=#{0}btnOk".Formato(popupPrefix)); //Dont't call PopupOk helper => it makes an ajaxWait and then waitPageLoad fails
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void ConstructFrom_Submit()
        {
            //Album.Clone
            CheckLoginAndOpen(ViewRoute("Album", 1));

            selenium.EntityMenuOptionClick(constructorsMenuId, "AlbumOperation_Clone");
            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.Type("Name", "test3");
            selenium.Type("Year", "2010");

            selenium.EntityButtonClick("AlbumOperation_Save");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void Delete()
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
        public void ConstructFromMany_OpenPopup()
        {
            //Album.CreateGreatestHits
            CheckLoginAndOpen("{0}?allowMultiple=true".Formato(FindRoute("Album")));

            selenium.Search();

            selenium.SelectRowCheckbox(0);
            selenium.SelectRowCheckbox(1);

            selenium.QueryMenuOptionClick(constructorsMenuId, "AlbumOperation_CreateEmptyGreatestHitsAlbum");
            string popupPrefix = "New_";
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(popupPrefix)));

            selenium.Type("{0}Name".Formato(popupPrefix), "test greatest empty");
            selenium.Select("{0}Label".Formato(popupPrefix), "label=Virgin");

            selenium.EntityButtonClick("AlbumOperation_Save");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.EntityButtonLocator("AlbumOperation_Modify")));
            Assert.IsTrue(selenium.IsElementPresent(SeleniumExtensions.PopupSelector(popupPrefix)));
        }

        [TestMethod]
        public void ConstructFromMany_Submit_UseSessionWhenNew()
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
