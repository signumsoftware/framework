using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Synchronization;
using System.Diagnostics;

namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for SearchControlLoaded.tsx
/// </summary>
public class SearchControlProxy
{
    public ILocator Locator { get; private set; }
    public ResultTableProxy Results { get; private set; }

    public object QueryName { get; private set; }

    public FiltersProxy Filters => new FiltersProxy(FiltersPanel, QueryName);
    public ColumnEditorProxy ColumnEditor => new ColumnEditorProxy(Locator.Locator(".sf-column-editor"));
    public PaginationSelectorProxy Pagination => new PaginationSelectorProxy(this);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public SearchControlProxy(ILocator locator, object queryName)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        Locator = locator;
        Results = new ResultTableProxy(Locator.Locator(".sf-scroll-table-container"), this);
        QueryName = queryName;
    }

    public static async Task<SearchControlProxy> NewAsync(ILocator locator, bool waitInitialSearch)
    {
        var queryName = QueryLogic.ToQueryName((await locator.GetAttributeAsync("data-query-key"))!);
        var sc = new SearchControlProxy(locator, queryName);
        if (waitInitialSearch)
            await sc.WaitInitialSearchCompletedAsync();

        return sc;
    }

    public ILocator SearchButton => Locator.Locator(".sf-query-button.sf-search");

    public async Task SearchAsync()
    {
        await WaitSearchCompletedAsync(() => SearchButton.ClickAsync());
    }

    public async Task WaitSearchCompletedAsync(Func<Task> searchTrigger)
    {
        var counter = await Locator.GetAttributeAsync("data-search-count");
        await searchTrigger();
        await WaitSearchCompletedAsync(counter);
    }

    public async Task WaitInitialSearchCompletedAsync()
    {
        await WaitSearchCompletedAsync((string?)null);
    }

    private async Task WaitSearchCompletedAsync(string? counter)
    {
        await Locator.WaitAttributeAsync("data-search-count", counter, "!==");
    }

    public async Task<ILocator> WaitContextMenuAsync()
    {
        var locator = Locator.Page.Locator(".sf-context-menu .dropdown-menu");

        await locator.WaitVisibleAsync();

        return locator;
    }

    public ILocator ToggleFiltersButton => Locator.Locator(".sf-filter-button");
    public ILocator FiltersPanel => Locator.Locator(".sf-filters-list");

    public async Task ToggleFiltersAsync(bool show)
    {
        await ToggleFiltersButton.ClickAsync();
        if (show)
            await FiltersPanel.WaitVisibleAsync();
        else
            await FiltersPanel.WaitNotVisibleAsync();
    }

    public ILocator ContextualMenu => Locator.Page.Locator(".sf-context-menu");

    public async Task<FilterConditionProxy> AddQuickFilterAsync(int rowIndex, string token)
    {
        var cell = await Results.CellElementAsync(rowIndex, token);
        await cell.ClickAsync(new() { Button = MouseButton.Right });

        var menuItem = ContextualMenu.Locator(".sf-quickfilter-header a");
        await menuItem.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        var filterProxy = await this.Filters.GetNewFilterAsync(() => menuItem.ClickAsync());

        return (FilterConditionProxy)filterProxy;
    }

    public async Task<FilterConditionProxy> AddQuickFilterAsync(string token)
    {
        await Results.HeaderCellElement(token).ClickAsync(new() { Button = MouseButton.Right });
        var menuItem = ContextualMenu.Locator(".sf-quickfilter-header a");
        await menuItem.WaitForAsync();
        var FilterProxy = await this.Filters.GetNewFilterAsync(() => menuItem.ClickAsync());
        return (FilterConditionProxy)FilterProxy;
    }

    public async Task<FrameModalProxy<T>> CreateAsync<T>() where T : ModifiableEntity
    {
        var modalLocator = await CreateButton.CaptureOnClickAsync();
        if (await SelectorModalProxy.IsSelectorAsync(modalLocator))
            modalLocator = await modalLocator.AsSelectorModal().SelectAndCaptureAsync<T>();
        var modal = await FrameModalProxy<T>.NewAsync(modalLocator);
        return modal;
    }

    public ILocator CreateButton => Locator.Locator(".sf-create");

    public async Task<bool> HasMultiplyMessageAsync() => await Locator.Locator(".sf-td-multiply").CountAsync() > 0;
    public async Task<bool> FiltersVisibleAsync() => await FiltersPanel.IsVisibleAsync();

    public ILineContainer<T> SimpleFilterBuilder<T>() where T : ModifiableEntity
    {
        return new LineContainer<T>(Locator.Locator(".simple-filter-builder"));
    }

 
}
