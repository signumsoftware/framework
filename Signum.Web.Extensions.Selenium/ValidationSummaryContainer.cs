using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public interface IValidationSummaryContainer
    {
        RemoteWebDriver Selenium { get; }

        string Prefix { get; }
    }

    public static class ValidationSummaryContainer
    {
        public static By ValidationSummaryLocator(this IValidationSummaryContainer container)
        {
            return By.CssSelector("#{0}_sfGlobalValidationSummary".FormatWith(container.Prefix));
        }

        public static bool FormHasNErrors(this IValidationSummaryContainer container, int? numberOfErrors)
        {
            var locator = container.ValidationSummaryLocator();

            if (numberOfErrors.HasValue)
            {
                return container.Selenium.IsElementPresent(locator.CombineCss(" > ul > li:nth-child({0})".FormatWith(numberOfErrors))) &&
                    !container.Selenium.IsElementPresent(locator.CombineCss(" > ul > li:nth-child({0})".FormatWith(numberOfErrors + 1)));
            }
            else
            {
                return container.Selenium.IsElementPresent(locator.CombineCss(" > ul > li"));
            }
        }
    }
}
