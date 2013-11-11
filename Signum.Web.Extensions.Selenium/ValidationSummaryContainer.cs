using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public interface IValidationSummaryContainer
    {
        ISelenium Selenium { get; }

        string Prefix { get; }
    }

    public static class ValidationSummaryContainer
    {
        public static string ValidationSummaryLocator(this IValidationSummaryContainer container)
        {
            return "jq=#{0}_sfGlobalValidationSummary".Formato(container.Prefix);
        }

        public static bool FormHasNErrors(this IValidationSummaryContainer container, int? numberOfErrors)
        {
            var locator = container.ValidationSummaryLocator();

            if (numberOfErrors.HasValue)
            {
                return container.Selenium.IsElementPresent("{0} > ul > li:nth-child({1})".Formato(locator, numberOfErrors)) &&
                       !container.Selenium.IsElementPresent("{0} > ul > li:nth-child({1})".Formato(locator, numberOfErrors + 1));
            }
            else
            {
                return container.Selenium.IsElementPresent("{0} > ul > li".Formato(locator));
            }
        }
    }
}
