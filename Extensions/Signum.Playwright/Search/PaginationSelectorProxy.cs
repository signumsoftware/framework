using Microsoft.Playwright;

namespace Signum.Playwright.Search;

public class PaginationSelectorProxy
{
    public SearchControlProxy SearchControl { get; }
    public ILocator Element { get; }
    public IPage Page => SearchControl.Page;

    public PaginationSelectorProxy(SearchControlProxy searchControl)
    {
        SearchControl = searchControl;
        Element = searchControl.Element.Locator(".sf-search-footer, .pagination-container");
    }

    public ILocator PaginationSelect => Element.Locator(".sf-pagination-mode select");
    public ILocator ElementsPerPageSelect => Element.Locator(".sf-elements-per-page select");
    public ILocator CurrentPageLabel => Element.Locator(".sf-pagination-info, .pagination-label");

    public async Task SetElementsPerPageAsync(int elements)
    {
        await ElementsPerPageSelect.SelectOptionAsync(elements.ToString());
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<int> GetElementsPerPageAsync()
    {
        var value = await ElementsPerPageSelect.InputValueAsync();
        return int.Parse(value);
    }

    public async Task SetPaginationModeAsync(string mode)
    {
        await PaginationSelect.SelectOptionAsync(new SelectOptionValue { Label = mode });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<string?> GetPaginationModeAsync()
    {
        return await PaginationSelect.InputValueAsync();
    }

    public async Task NextPageAsync()
    {
        var nextButton = Element.Locator(".pagination .page-link:has-text('›'), button:has-text('Next')");
        await nextButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task PreviousPageAsync()
    {
        var prevButton = Element.Locator(".pagination .page-link:has-text('‹'), button:has-text('Previous')");
        await prevButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GoToPageAsync(int pageNumber)
    {
        var pageButton = Element.Locator($".pagination .page-link:has-text('{pageNumber}')");
        await pageButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<string?> GetCurrentPageInfoAsync()
    {
        return await CurrentPageLabel.TextContentAsync();
    }
}
