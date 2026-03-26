namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for QueryTokenPart in QueryTokenBuilder.tsx
/// </summary>
public class QueryTokenPartProxy
{
    public ILocator Element { get; }

    public QueryTokenPartProxy(ILocator element)
    {
        Element = element;
    }

    public async Task SelectAsync(string? fullKey)
    {
        var isVisible = await Element.Locator(".rw-popup-container").IsVisibleAsync();
        if (!isVisible)
        {
            await Element.Locator(".rw-dropdown-list, .sf-query-token-plus").ClickAsync();
        }

        var dropdownContainer = Element.Locator(".rw-popup-container");
        await dropdownContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        var tokenSelector = !string.IsNullOrEmpty(fullKey) ? $"[data-full-token='{fullKey}']" : ":not([0])";
        var optionElement = dropdownContainer.Locator($"div > span{tokenSelector}");
        await optionElement.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await optionElement.ClickAsync();

        await Element.Locator($".rw-dropdown-list-value span{tokenSelector}")
                     .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}
