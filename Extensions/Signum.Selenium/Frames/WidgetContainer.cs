using OpenQA.Selenium;
using Signum.Utilities;

namespace Signum.React.Selenium;

public interface IWidgetContainer
{
    IWebElement Element { get; }
}

public static class WidgetContainerExtensions
{
    public static WebElementLocator WidgetContainer(this IWidgetContainer container)
    {
        return container.Element.WithLocator(By.CssSelector("ul.sf-widgets"));
    }

    public static IWebElement QuickLinkClick(this IWidgetContainer container, string name)
    {
        var ql = container.WidgetContainer().CombineCss("dropdown .sf-quicklinks").Find();

        ql.Click();

        var element = ql.GetParent().WaitElementPresent(By.CssSelector("ul.dropdown-menu a[data-name='{0}']".FormatWith(name)));

        return element.CaptureOnClick();
    }

    public static SearchModalProxy QuickLinkClickSearch(this IWidgetContainer container, string name)
    {
        return new SearchModalProxy(container.QuickLinkClick(name));
    }
}
