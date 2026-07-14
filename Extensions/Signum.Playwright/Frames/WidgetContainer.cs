using Signum.Playwright.Search;

namespace Signum.Playwright.Frames;

/// <summary>
/// Interface for proxies of components calling Widgets.tsx
/// </summary>
public interface IWidgetContainer
{
    ILocator Element { get; }
    IPage Page { get; }
}

public static class WidgetContainerExtensions
{
    public static ILocator WidgetContainer(this IWidgetContainer container)
    {
        return container.Element.Locator("ul.sf-widgets");
    }

    public static async Task<ILocator> QuickLinkClickAsync(this IWidgetContainer container, string name)
    {
        var ql = container.WidgetContainer().Locator(".dropdown .sf-quicklinks");

        await ql.ClickAsync();

        var element = ql
            .Locator("xpath=..")
            .Locator("ul.dropdown-menu a[data-name='{0}']".FormatWith(name));

        await element.ClickAsync();

        return element;
    }

    public static async Task<SearchModalProxy> QuickLinkClickSearchAsync(this IWidgetContainer container, string name)
    {
        var ql = container.WidgetContainer().Locator(".dropdown .sf-quicklinks");

        await ql.ClickAsync();

        var element = ql
            .Locator("xpath=..")
            .Locator($"ul.dropdown-menu a[data-name='{name}']");

        var modal = await container.Page.CaptureModalAsync(() => element.ClickAsync());

        return await SearchModalProxy.NewAsync(modal);
    }
}
