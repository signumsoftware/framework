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
using Signum.Entities.UserQueries;
using System.Text.RegularExpressions;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class UserQueriesTests : Common
    {
        public UserQueriesTests()
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
        public void UserQueries001_Create()
        {
            string pathAlbumSearch = FindRoute("Album");

            CheckLoginAndOpen(pathAlbumSearch);

            //add filter of simple value
            selenium.FilterSelectToken(0, "label=Year", false);
            selenium.AddFilter(0);
            selenium.FilterSelectOperation(0, "value=GreaterThan");
            selenium.Type("value_0", "2000");

            //add filter of lite
            selenium.FilterSelectToken(0, "label=Label", true);
            selenium.AddFilter(0);
            selenium.LineFind("value_1_");
            selenium.Sort(3, true, "value_1_");
            selenium.Search("value_1_");
            selenium.SelectRowCheckbox(1, "value_1_");
            selenium.PopupOk("value_1_");

            //add user column
            selenium.FilterSelectToken(1, "label=Owner", true);
            selenium.AddColumn("Label.Owner");

            int yearCol = 6;

            //add order
            selenium.Sort(yearCol, true);
            selenium.Sort(yearCol, false);
            
            string uqMenuId = "tmUserQueries";
            string uqCreateId = "qbUserQueryNew";

            //create user query
            selenium.QueryMenuOptionClick(uqMenuId, uqCreateId);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.Type("DisplayName", "Last albums");

            selenium.EntityOperationClick(UserQueryOperation.Save);
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //check new user query is in the dropdownlist
            string uqOptionSelector = "title='Last albums'";
            selenium.Open(pathAlbumSearch);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.QueryMenuOptionPresentByAttr(uqMenuId, uqOptionSelector, true);

            //load user query
            selenium.Click(SearchTestExtensions.QueryMenuOptionLocatorByAttr(uqMenuId, uqOptionSelector));
            selenium.WaitForPageToLoad(PageLoadTimeout);
            //Filter present
            Assert.IsTrue(selenium.IsElementPresent("value_0"));
            Assert.IsTrue(selenium.IsElementPresent(LinesTestExtensions.EntityLineToStrSelector("value_1_")));
            //Column present
            selenium.TableHasColumn("Label.Owner");
            //Sort present
            selenium.TableHeaderMarkedAsSorted(yearCol, false, true);
        }

        [TestMethod]
        public void UserQueries002_Edit()
        {
            string pathAlbumSearch = FindRoute("Album");

            CheckLoginAndOpen(pathAlbumSearch);

            string uqMenuId = "tmUserQueries";
            string uqOptionSelector = "title='Last albums'";
            string editId = "qbUserQueryEdit";

            //load user query
            selenium.Click(SearchTestExtensions.QueryMenuOptionLocatorByAttr(uqMenuId, uqOptionSelector));
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //edit it
            selenium.QueryMenuOptionClick(uqMenuId, editId);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            //remove filter
            selenium.LineRemove("Filters_1_");
            //add column
            selenium.LineCreate("Columns_", false, 1);
            string prefix = "Columns_1_";
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(prefix + "DisplayName"));
            selenium.Type(prefix + "DisplayName", "Label owner's country");
            selenium.FilterSelectToken(0, "value=Label", true, prefix);
            selenium.FilterSelectToken(1, "value=Owner", true, prefix);
            selenium.FilterSelectToken(2, "value=Country", true, prefix);

            //save it
            selenium.EntityOperationClick(UserQueryOperation.Save);
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //check new user query is in the dropdownlist
            selenium.Open(pathAlbumSearch);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            
            //load user query
            selenium.Click(SearchTestExtensions.QueryMenuOptionLocatorByAttr(uqMenuId, uqOptionSelector));
            selenium.WaitForPageToLoad(PageLoadTimeout);
            //Filter present
            Assert.IsTrue(selenium.IsElementPresent("value_0"));
            Assert.IsFalse(selenium.IsElementPresent(LinesTestExtensions.EntityLineToStrSelector("value_1_"))); //Filter has been removed
            //Column present
            selenium.TableHasColumn("Label.Owner");
            //New column present
            selenium.TableHasColumn("Label.Owner.Country");
            //Sort present
            int yearCol = 6;
            selenium.TableHeaderMarkedAsSorted(yearCol, false, true);
        }

        [TestMethod]
        public void UserQueries003_Delete()
        {
            string pathAlbumSearch = FindRoute("Album");

            string uqMenuId = "tmUserQueries";
            string uqCreateId = "qbUserQueryNew";
            string editId = "qbUserQueryEdit";

            CheckLoginAndOpen(pathAlbumSearch);

            int yearCol = 6;

            //add order
            selenium.Sort(yearCol, true);
            
            //create user query
            selenium.QueryMenuOptionClick(uqMenuId, uqCreateId);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.Type("DisplayName", "test");

            selenium.EntityOperationClick(UserQueryOperation.Save);
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //check new user query is in the dropdownlist
            string uqOptionSelector = "title='test'";
            selenium.Open(pathAlbumSearch);
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.QueryMenuOptionPresentByAttr(uqMenuId, uqOptionSelector, true);

            //load user query
            selenium.Click(SearchTestExtensions.QueryMenuOptionLocatorByAttr(uqMenuId, uqOptionSelector));
            selenium.WaitForPageToLoad(PageLoadTimeout);
            //Sort present
            selenium.TableHeaderMarkedAsSorted(yearCol, true, true);

            //edit it
            selenium.QueryMenuOptionClick(uqMenuId, editId);
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //remove it
            selenium.EntityOperationClick(UserQueryOperation.Delete);
            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.QueryMenuOptionPresentByAttr(uqMenuId, uqOptionSelector, false);
        }
    }
}
