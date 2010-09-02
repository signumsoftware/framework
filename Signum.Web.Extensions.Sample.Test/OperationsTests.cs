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

                selenium.Type("Name", "prueba");
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

                selenium.Type("New_Name", "prueba2");
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
                
                selenium.Type("Name", "prueba3");
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
    }
}
