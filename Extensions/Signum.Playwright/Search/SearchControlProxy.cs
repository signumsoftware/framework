using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Synchronization;
using System.Diagnostics;

namespace Signum.Playwright.Search;

public class SearchControlProxy
{
    public ILocator Element { get; private set; }
    public ResultTableProxy Results { get; private set; }

    public object QueryName { get; private set; }

    public FiltersProxy Filters => new FiltersProxy(FiltersPanel, QueryName);
    public ColumnEditorProxy ColumnEditor => new ColumnEditorProxy(Element.Locator(".sf-column-editor"));
    public PaginationSelectorProxy Pagination => new PaginationSelectorProxy(this);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public SearchControlProxy(ILocator element)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        Element = element;
        Results = new ResultTableProxy(Element.Locator(".sf-scroll-table-container"), this);
    }

    public async Task Initialize()
    {
        QueryName = QueryLogic.ToQueryName((await Element.GetAttributeAsync("data-query-key"))!);
    }

    public ILocator SearchButton => Element.Locator(".sf-query-button.sf-search");

    public async Task SearchAsync()
    {
        await WaitSearchCompletedAsync(() => SearchButton.ClickAsync());
    }

    public async Task WaitSearchCompletedAsync(Func<Task> searchTrigger)
    {
        var counter = await Element.GetAttributeAsync("data-search-count");
        await searchTrigger();
        await WaitSearchCompletedAsync(counter);
    }

    public async Task WaitInitialSearchCompletedAsync()
    {
        await WaitSearchCompletedAsync((string?)null);
    }

    private async Task WaitSearchCompletedAsync(string? counter)
    {
        await Element.WaitAttributeAsync("data-search-count", counter, "!==");
    }

    public async Task<EntityContextMenuProxy> SelectedClickAsync()
    {
        await Element.Locator(".sf-tm-selected").ClickAsync();
        var menu = Element.Locator("div.dropdown > .dropdown-menu");
        await menu.WaitForAsync();
        return new EntityContextMenuProxy(Results, menu);
    }

    public async Task<ILocator> WaitContextMenuAsync()
    {
        var locator = Element.Page.Locator(".sf-context-menu .dropdown-menu");

        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        return locator;
    }

    public ILocator ToggleFiltersButton => Element.Locator(".sf-filter-button");
    public ILocator FiltersPanel => Element.Locator(".sf-filters-list");

    public async Task ToggleFiltersAsync(bool show)
    {
        await ToggleFiltersButton.ClickAsync();
        if (show)
            await FiltersPanel.WaitVisibleAsync();
        else
            await FiltersPanel.WaitNotVisibleAsync();
    }

    public ILocator ContextualMenu => Element.Page.Locator(".sf-context-menu");

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
        var modal = await new FrameModalProxy<T>(modalLocator).WaitLoadedAsync();
        return modal;
    }

    public ILocator CreateButton => Element.Locator(".sf-create");

    public async Task<bool> HasMultiplyMessageAsync() => await Element.Locator(".sf-td-multiply").CountAsync() > 0;
    public async Task<bool> FiltersVisibleAsync() => await FiltersPanel.IsVisibleAsync();

    public ILineContainer<T> SimpleFilterBuilder<T>() where T : ModifiableEntity
    {
        return new LineContainer<T>(Element.Locator(".simple-filter-builder"));
    }
}
