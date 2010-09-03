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

        [TestMethod]
        public void Execute_Navigate()
        {
            try
            {
                //Album.Save
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album");

                selenium.Click("jq=input.create");
                selenium.WaitForPageToLoad(PageLoadTimeout);

                selenium.Type("Name", "test");
                selenium.Type("Year", "2010");
                selenium.Click("Author_btnFind");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #AuthorTemp"));
                selenium.Click("BandDN");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Author_btnSearch"));
                selenium.Click("Author_btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Author_tblResults > tbody > tr"));
                selenium.Click("Author_rowSelection");
                selenium.Click("Author_sfBtnOk");
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("jq=#divASustituir + #AuthorTemp"));
                selenium.Select("Label", "label=Virgin");
                
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
        public void Execute_ReloadContent()
        {
            try
            {
                //Album.Modify
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/View/Album/1");

                selenium.Type("Name", "Siamese Dreamm");
                selenium.Click("AlbumOperation_Modify");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("//span[contains(@class,'title') and text()='Siamese Dreamm']"));

                //Restore state for future tests
                selenium.Type("Name", "Siamese Dream");
                selenium.Click("AlbumOperation_Modify");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("//span[contains(@class,'title') and text()='Siamese Dream']"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }

        [TestMethod]
        public void ConstructFrom_OpenPopup()
        {
            try
            {
                //Album.CreateFromBand
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/View/Band/1");

                selenium.Click("AlbumOperation_CreateFromBand");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #NewTemp"));

                selenium.Type("New_Name", "test2");
                selenium.Type("New_Year", "2010");
                selenium.Click("New_Label_btnFind");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #New_LabelTemp"));
                selenium.Click("New_Label_btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#New_Label_tblResults > tbody > tr"));
                selenium.Click("New_Label_rowSelection");
                selenium.Click("New_Label_sfBtnOk");
                selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("jq=#divASustituir + #New_LabelTemp"));

                //When clicking Ok (Save) => Custom controller that returns a url => navigate
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
        public void ConstructFrom_Submit()
        {
            try
            {
                //Album.Clone
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/View/Album/1");

                selenium.Click("AlbumOperation_Clone");
                selenium.WaitForPageToLoad(PageLoadTimeout);
                
                selenium.Type("Name", "test3");
                selenium.Type("Year", "2010");
                
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
        public void Delete()
        {
            try
            {
                //Album.Delete
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/View/Album/13");
                
                Assert.IsTrue(selenium.IsElementPresent("AlbumOperation_Delete"));
                selenium.Click("AlbumOperation_Delete");
                selenium.WaitAjaxFinished(() => selenium.IsConfirmationPresent());
                string confirmation = selenium.GetConfirmation();
                Assert.IsTrue(Regex.IsMatch(confirmation, ".*"));
                //Assert.AreEqual("Confirme que desea eliminar la entidad del sistema", selenium.GetConfirmation());

                selenium.WaitForPageToLoad(PageLoadTimeout);
                
                //Delete has redirected to search window => Check deleted album doesn't exist any more
                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr"));
                Assert.IsFalse(selenium.IsElementPresent("jq=a[href=View/Album/13]"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }

        [TestMethod]
        public void ConstructFromMany_OpenPopup()
        {
            try
            {
                //Album.CreateGreatestHits
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album?sfAllowMultiple=true");

                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr"));

                selenium.Click("rowSelection_0");
                selenium.Click("rowSelection_1");
                selenium.Click("AlbumOperation_CreateEmptyGreatestHitsAlbum");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#divASustituir + #NewTemp"));

                selenium.Type("New_Name", "test greatest empty");
                selenium.Select("New_Label", "label=Virgin");

                selenium.Click("AlbumOperation_Save");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#AlbumOperation_Modify"));
                Assert.IsTrue(selenium.IsElementPresent("jq=#divASustituir + #NewTemp"));
            }
            catch (Exception)
            {
                Common.MyTestCleanup();
                throw;
            }
        }

        [TestMethod]
        public void ConstructFromMany_Submit_UseSessionWhenNew()
        {
            try
            {
                //Album.CreateGreatestHits
                CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Find/Album?sfAllowMultiple=true");

                selenium.Click("btnSearch");
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults > tbody > tr"));

                selenium.Click("rowSelection_0");
                selenium.Click("rowSelection_1");
                selenium.Click("AlbumOperation_CreateGreatestHitsAlbum");
                selenium.WaitForPageToLoad(PageLoadTimeout);

                selenium.Type("Name", "test greatest hits");
                selenium.Select("Label", "label=Virgin");
                
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
    }
}
