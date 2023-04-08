using System.Linq;
using OpenQA.Selenium;

namespace Signum.React.Selenium;

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
