using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Support.UI;
using Signum.Utilities;


namespace Signum.Web.Selenium
{
    public static class SeleniumExtensions
    {
        //public static string PageLoadTimeout = "20000";
        public static TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(20 * 1000);
        public static TimeSpan DefaultPoolingInterval = TimeSpan.FromMilliseconds(200);

        //public static void WaitForPageToLoad(this RemoteWebDriver selenium)
        //{
        //    selenium.WaitForPageToLoad(PageLoadTimeout);
        //}

        public static T Wait<T>(this RemoteWebDriver selenium, Func<T> condition, Func<string> actionDescription = null, TimeSpan? timeout = null)
        {
            try
            {
                var wait = new DefaultWait<string>(null)
                {
                    Timeout = timeout ?? DefaultTimeout,
                    PollingInterval = DefaultPoolingInterval
                };
                
                return wait.Until(_ => condition());
            }
            catch (WebDriverTimeoutException ex)
            {
                throw new WebDriverTimeoutException(ex.Message + ": waiting for {0} in page {1}({2})".FormatWith(
                    actionDescription == null ? "visual condition" : actionDescription(),
                    selenium.Title,
                    selenium.Url));
            }
        }

        public static IWebElement WaitElementPresent(this RemoteWebDriver selenium, By locator, Func<string> actionDescription = null, TimeSpan? timeout = null)
        {
            return selenium.Wait(() => selenium.FindElement(locator),
                actionDescription ?? (Func<string>)(() => "{0} to be present".FormatWith(locator)), timeout);
        }

        public static void WaitElementDisapear(this RemoteWebDriver selenium, By locator, Func<string> actionDescription = null, TimeSpan? timeout = null)
        {
            selenium.Wait(() => !selenium.IsElementPresent(locator), 
                actionDescription ?? (Func<string>)(() => "{0} to disapear".FormatWith(locator)), timeout);
        }

        public static void AssertElementPresent(this RemoteWebDriver selenium, By locator)
        {
            if (!selenium.IsElementPresent(locator))
                throw new InvalidOperationException("{0} not found".FormatWith(locator));
        }

        public static bool IsElementPresent(this RemoteWebDriver selenium, By locator)
        {
            return selenium.FindElements(locator).Any();
        }

        public static void AssertElementNotPresent(this RemoteWebDriver selenium, By locator)
        {
            if (selenium.IsElementPresent(locator))
                throw new InvalidOperationException("{0} is found".FormatWith(locator));
        }

        public static void SetChecked(this RemoteWebDriver selenium, By locator, bool isChecked)
        {
            var element = selenium.FindElement(locator);

            if (element.Selected == isChecked)
                return;

            element.Click();

            if (element.Selected != isChecked)
                throw new InvalidOperationException();
        }

        public static bool IsAlertPresent(this RemoteWebDriver selenium)
        {
            try
            {
                selenium.SwitchTo().Alert();
                return true;
            }
            catch (NoAlertPresentException e)
            {
                return false;
            }
        }

        public static void ConsumeAlert(this RemoteWebDriver selenium)
        {
            selenium.Wait(() => selenium.SwitchTo().Alert()).Accept();
        }

        public static string CssSelector(this By by)
        {
            string str = by.ToString();

            var after = str.After(": ");
            switch (str.Before(":"))
            {
                case "By.CssSelector": return after;
                case "By.Id": return "#" + after;
                case "By.Name": return "[name=" + after + "]";
                default: throw new InvalidOperationException("Impossible to combine: " + str);
            }
        }

        public static By CombineCss(this By by, string cssSelectorSuffix)
        {
            return By.CssSelector(by.CssSelector() + cssSelectorSuffix);
        }

        public static SelectElement SelectElement(this IWebElement element)
        {
            return new SelectElement(element);
        }

        public static void SelectByPredicate(this SelectElement element, Func<IWebElement, bool> predicate)
        {
            element.AllSelectedOptions.SingleEx(predicate).Click();
        }

        public static void ContextClick(this IWebElement element)
        {
            //Astronautical architects turn back to Houston...
            ((RemoteWebDriver)((RemoteWebElement)element).WrappedDriver).Mouse.ContextClick(((ILocatable)element).Coordinates);
        }

        public static void DoubleClick(this IWebElement element)
        {
            //Astronautical architects turn back to Houston...
            ((RemoteWebDriver)((RemoteWebElement)element).WrappedDriver).Mouse.DoubleClick(((ILocatable)element).Coordinates);
        }
    }
}
