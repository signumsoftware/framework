using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public interface IValidationSummaryContainer
    {
        IWebElement Element { get; }
    }

    public static class ValidationSummaryContainerExtensions
    {
        public static WebElementLocator ValidationSummary(this IValidationSummaryContainer container)
        {
            return container.Element.WithLocator(By.CssSelector("ul.validaton-summary"));
        }

        public static string[] ValidationErrors(this IValidationSummaryContainer container)
        {
            var errors = container.ValidationSummary().CombineCss(" > li").FindElements().Select(a => a.Text).ToArray();

            return errors;
        }
    }
}
