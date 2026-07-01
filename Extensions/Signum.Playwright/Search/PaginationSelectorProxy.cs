namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for PaginationSelector.tsx
/// </summary>
public class PaginationSelectorProxy
{
    private readonly SearchControlProxy searchControl;
    public ILocator Element { get; }

    public PaginationSelectorProxy(SearchControlProxy searchControl)
    {
        this.searchControl = searchControl;
        Element = searchControl.Locator.Locator(".sf-search-footer");
    }

    public ILocator ElementsPerPageElement => Element.Locator("select.sf-elements-per-page");

    public async Task SetElementsPerPageAsync(int elementsPerPage)
    {
        await searchControl.WaitSearchCompletedAsync(async () =>
        {
            var select = ElementsPerPageElement;
            await select.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await select.SelectOptionAsync(elementsPerPage.ToString());
        });
    }

    public ILocator PaginationModeElement => Element.Locator("select.sf-pagination-mode");

    public async Task SetPaginationModeAsync(PaginationMode mode)
    {
        var select = PaginationModeElement;
        await select.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await select.SelectOptionAsync(mode.ToString());
    }
}
