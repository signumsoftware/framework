using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.Web.Selenium;

namespace Signum.Web.Selenium
{
    public static class SearchControlExcelExtensions
    {
        public static SearchPageProxy AdministerExcelReports(this SearchControlProxy sc)
        {
            sc.Selenium.Click(sc.MenuOptionLocator("tmExcel", "qbReportAdminister"));
            sc.Selenium.WaitForPageToLoad(SeleniumExtensions.DefaultPageLoadTimeout);
            return new SearchPageProxy(sc.Selenium);
        }

        public static string ExcelReportLocator(this SearchControlProxy sc, string title)
        {
            return sc.MenuOptionLocatorByAttr("tmExcel", "title='{0}'".Formato(title));
        }

        public static NormalPage<UserQueryDN> NewUserQuery(this SearchControlProxy sc)
        {
            sc.Selenium.Click(sc.MenuOptionLocator("tmUserQueries", "qbUserQueryNew"));
            sc.Selenium.WaitForPageToLoad(SeleniumExtensions.DefaultPageLoadTimeout);

            return new NormalPage<UserQueryDN>(sc.Selenium); 
        }

        public static NormalPage<UserQueryDN> EditUserQuery(this SearchControlProxy sc)
        {
            sc.Selenium.Click(sc.MenuOptionLocator("tmUserQueries", "qbUserQueryEdit"));
            sc.Selenium.WaitForPageToLoad(SeleniumExtensions.DefaultPageLoadTimeout);

            return new NormalPage<UserQueryDN>(sc.Selenium);
        }

        public static string UserQueryLocator(this SearchControlProxy sc, string title)
        {
            return sc.MenuOptionLocatorByAttr("tmUserQueries", "title='{0}'".Formato(title));
        }

        public static void UserQueryLocatorClick(this SearchControlProxy sc, string title)
        {
            sc.Selenium.Click(sc.UserQueryLocator(title));
            sc.Selenium.WaitElementPresent(sc.MenuOptionLocator("tmUserQueries", "qbUserQueryEdit"));
        }
    }
}
