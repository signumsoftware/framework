using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using Signum.Entities.Excel;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.React.Selenium;

namespace Signum.React.Selenium
{
    public static class SearchControlExcelExtensions
    {
        public static SearchPopupProxy AdministerExcelReports(this SearchControlProxy sc)
        {
            sc.Selenium.FindElement(sc.MenuOptionLocator("qbReportAdminister")).ButtonClick();
            return new SearchPopupProxy(sc.Selenium, sc.PrefixUnderscore + "New");
        }

        public static PopupControl<ExcelReportEntity> CreateExcelReport(this SearchControlProxy sc)
        {
            sc.Selenium.FindElement(sc.MenuOptionLocator("qbReportCreate")).ButtonClick();
            return new PopupControl<ExcelReportEntity>(sc.Selenium, sc.PrefixUnderscore + "New");
        }

        public static IWebElement ExcelReportElement(this SearchControlProxy sc, string title)
        {
            return sc.MenuOptionLocatorByAttr("title='{0}'".FormatWith(title));
        }

        public static NormalPage<UserQueryEntity> NewUserQuery(this SearchControlProxy sc)
        {
            sc.Selenium.FindElement(sc.MenuOptionLocator("qbUserQueryNew")).ButtonClick();

            return new NormalPage<UserQueryEntity>(sc.Selenium).WaitLoaded(); 
        }

        public static NormalPage<UserQueryEntity> EditUserQuery(this SearchControlProxy sc)
        {
            sc.Selenium.FindElement(sc.MenuOptionLocator("qbUserQueryEdit")).ButtonClick();

            return new NormalPage<UserQueryEntity>(sc.Selenium).WaitLoaded();
        }

        public static IWebElement UserQueryElement(this SearchControlProxy sc, string title)
        {
            return sc.MenuOptionLocatorByAttr("title='{0}'".FormatWith(title));
        }

        public static void UserQueryLocatorClick(this SearchControlProxy sc, string title)
        {
            sc.Selenium.FindElement(sc.UserQueryLocator(title)).ButtonClick();
            sc.Selenium.WaitElementPresent(sc.MenuOptionLocator("qbUserQueryEdit"));
        }
    }
}
