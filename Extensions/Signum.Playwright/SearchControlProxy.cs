using Microsoft.Playwright;
using Signum.Playwright.Search;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright;

/// <summary>
/// Playwright equivalent of Selenium's SearchControlProxy
/// Handles search controls in Signum Framework
/// </summary>
public class SearchControlProxy
{
    public IPage Page { get; }
    public ILocator Element { get; }

    public SearchControlProxy(IPage page)
    {
        Page = page;
        Element = page.Locator("[data-search-control], .sf-search-control").First;
    }

    public SearchControlProxy(ILocator element, IPage page)
    {
        Page = page;
        Element = element;
    }

    public async Task<string> GetQueryKeyAsync()
    {
        return await Element.GetAttributeAsync("data-query-key") ?? "";
    }

    public async Task<object> GetQueryNameAsync()
    {
        var queryKey = await GetQueryKeyAsync();
        return QueryLogic.ToQueryName(queryKey);
    }

    public FiltersProxy Filters => new FiltersProxy(
        Element.Locator(".sf-filters-list"),
        Page,
        GetQueryNameAsync().Result
    );

    public PaginationSelectorProxy Pagination => new PaginationSelectorProxy(this);

    public PlaywrightResultTableProxy Results => new PlaywrightResultTableProxy(
        Element.Locator(".sf-scroll-table-container, .sf-search-results"),
        this
    );

    public ILocator SearchButton => Element.Locator("button.sf-query-button.sf-search, button:has-text('Search')");
    public ILocator CreateButton => Element.Locator("a.sf-create, button.sf-create");
    public ILocator ToggleFiltersButton => Element.Locator(".sf-filter-button, button:has-text('Filters')");
    public ILocator FiltersPanel => Element.Locator(".sf-filters-list");

    public async Task WaitInitialSearchCompletedAsync()
    {
        await WaitSearchCompletedAsync((string?)null);
    }

    public async Task SearchAsync()
    {
        await WaitSearchCompletedAsync(async () =>
        {
            await SearchButton.ClickAsync();
        });
    }

    public async Task SearchAsync(string searchText)
    {
        await Element.Locator("input[name='searchText'], .simple-filter-builder input").First.FillAsync(searchText);
        await SearchAsync();
    }

    public async Task WaitSearchCompletedAsync(Func<Task>? searchTrigger)
    {
        var counter = await Element.GetAttributeAsync("data-search-count");
        
        if (searchTrigger != null)
            await searchTrigger();

        await WaitSearchCompletedAsync(counter);
    }

    private async Task WaitSearchCompletedAsync(string? counter)
    {
        var timeout = 20000;
        var start = DateTime.Now;

        while ((DateTime.Now - start).TotalMilliseconds < timeout)
        {
            var currentCounter = await Element.GetAttributeAsync("data-search-count");
            if (currentCounter != counter)
                return;

            await Task.Delay(200);
        }

        throw new TimeoutException($"Search did not complete within {timeout}ms");
    }

    public async Task<EntityContextMenuProxy> SelectedClickAsync()
    {
        await Element.Locator(".sf-tm-selected").ClickAsync();
        var menu = Element.Locator("div.dropdown > .dropdown-menu");
        await menu.WaitVisibleAsync();

        var resultTableElement = Element.Locator(".sf-scroll-table-container, .sf-search-results");
        var resultTableProxy = new ResultTableProxy(resultTableElement, this);
        return new EntityContextMenuProxy(resultTableProxy, menu);
    }

    public async Task<ILocator> WaitContextMenuAsync()
    {
        var menu = Page.Locator(".sf-context-menu .dropdown-menu");
        await menu.WaitVisibleAsync();
        return menu;
    }

    public async Task ToggleFiltersAsync(bool show)
    {
        await ToggleFiltersButton.ClickAsync();
        
        if (show)
            await FiltersPanel.WaitVisibleAsync();
        else
            await FiltersPanel.WaitNotVisibleAsync();
    }

    public async Task<bool> GetFiltersVisibleAsync()
    {
        return await FiltersPanel.IsVisibleAsync();
    }

    public async Task<FilterConditionProxy> AddQuickFilterAsync(int rowIndex, string token)
    {
        var cell = Results.CellElement(rowIndex, token);
        await cell.First.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        var menu = await WaitContextMenuAsync();
        var menuItem = menu.Locator(".sf-quickfilter-header a");

        return await Filters.GetNewFilterAsync(async () => await menuItem.ClickAsync());
    }

    public async Task<FilterConditionProxy> AddQuickFilterAsync(string token)
    {
        var header = Results.HeaderCellElement(token);
        await header.First.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        var menu = await WaitContextMenuAsync();
        var menuItem = menu.Locator(".sf-quickfilter-header a");

        return await Filters.GetNewFilterAsync(async () => await menuItem.ClickAsync());
    }

    public async Task<FrameModalProxy<T>> CreateAsync<T>() where T : ModifiableEntity
    {
        var popup = await ModalProxy.CaptureAsync(Page, async () =>
        {
            await CreateButton.ClickAsync();
        });

        if (await SelectorModalProxy.IsSelectorAsync(popup.Modal))
        {
            var selector = popup.AsSelectorModal();
            await selector.SelectAsync<T>();
        }

        return new FrameModalProxy<T>(Page, popup.Modal);
    }

    public async Task<bool> HasMultiplyMessageAsync()
    {
        return await Element.Locator(".sf-td-multiply").IsPresentAsync();
    }

    public LineContainer<T> SimpleFilterBuilder<T>() where T : ModifiableEntity
    {
        var filterBuilder = Element.Locator(".simple-filter-builder");
        return new LineContainer<T>(filterBuilder, Page);
    }
}

/// <summary>
/// Result table proxy for search results
/// </summary>
public class PlaywrightResultTableProxy
{
    public ILocator Element { get; }
    public SearchControlProxy SearchControl { get; }

    public PlaywrightResultTableProxy(ILocator element, SearchControlProxy searchControl)
    {
        Element = element;
        SearchControl = searchControl;
    }

    public ILocator Rows => Element.Locator("table.sf-search-results tbody tr, table tbody tr");

    public async Task<int> GetRowCountAsync()
    {
        return await Rows.CountAsync();
    }

    public ILocator GetRow(int index)
    {
        return Rows.Nth(index);
    }

    public ILocator CellElement(int rowIndex, string token)
    {
        return GetRow(rowIndex).Locator($"td[data-column-name='{token}'], td[data-token='{token}']");
    }

    public ILocator HeaderCellElement(string token)
    {
        return Element.Locator($"table thead th[data-column-name='{token}'], table thead th[data-token='{token}']");
    }

    public async Task<FramePageProxy<T>> EntityClickAsync<T>(int rowIndex) where T : Entity
    {
        var row = GetRow(rowIndex);
        var link = row.Locator("td a").First;
        
        await link.ClickAsync();
        await SearchControl.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        return new FramePageProxy<T>(SearchControl.Page);
    }

    public async Task EntityClickAsync(int rowIndex)
    {
        var row = GetRow(rowIndex);
        var link = row.Locator("td a").First;
        
        await link.ClickAsync();
        await SearchControl.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<List<string[]>> GetAllDataAsync()
    {
        var data = new List<string[]>();
        var rowCount = await GetRowCountAsync();

        for (int i = 0; i < rowCount; i++)
        {
            var row = GetRow(i);
            var cells = row.Locator("td");
            var cellCount = await cells.CountAsync();

            var rowData = new string[cellCount];
            for (int j = 0; j < cellCount; j++)
            {
                rowData[j] = (await cells.Nth(j).TextContentAsync())?.Trim() ?? "";
            }

            data.Add(rowData);
        }

        return data;
    }
}
