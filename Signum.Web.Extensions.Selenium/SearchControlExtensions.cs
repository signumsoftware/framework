using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Excel;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.Web.Selenium;

namespace Signum.Web.Selenium
{
    public static class SearchControlExcelExtensions
    {
        public static SearchPopupProxy AdministerExcelReports(this SearchControlProxy sc)
        {
            sc.Selenium.MouseUp(sc.MenuOptionLocator("qbReportAdminister"));
            return new SearchPopupProxy(sc.Selenium, sc.PrefixUnderscore + "New");
        }

        public static PopupControl<ExcelReportEntity> CreateExcelReport(this SearchControlProxy sc)
        {
            sc.Selenium.MouseUp(sc.MenuOptionLocator("qbReportCreate"));
            return new PopupControl<ExcelReportEntity>(sc.Selenium, sc.PrefixUnderscore + "New");
        }

        public static string ExcelReportLocator(this SearchControlProxy sc, string title)
        {
            return sc.MenuOptionLocatorByAttr("title='{0}'".FormatWith(title));
        }

        public static NormalPage<UserQueryEntity> NewUserQuery(this SearchControlProxy sc)
        {
            sc.Selenium.MouseUp(sc.MenuOptionLocator("qbUserQueryNew"));
            sc.Selenium.WaitForPageToLoad();

            return new NormalPage<UserQueryEntity>(sc.Selenium); 
        }

        public static NormalPage<UserQueryEntity> EditUserQuery(this SearchControlProxy sc)
        {
            sc.Selenium.Click(sc.MenuOptionLocator("qbUserQueryEdit"));
            sc.Selenium.WaitForPageToLoad();

            return new NormalPage<UserQueryEntity>(sc.Selenium);
        }

        public static string UserQueryLocator(this SearchControlProxy sc, string title)
        {
            return sc.MenuOptionLocatorByAttr("title='{0}'".FormatWith(title));
        }

        public static void UserQueryLocatorClick(this SearchControlProxy sc, string title)
        {
            sc.Selenium.Click(sc.UserQueryLocator(title));
            sc.Selenium.WaitElementPresent(sc.MenuOptionLocator("qbUserQueryEdit"));
        }
    }
}
