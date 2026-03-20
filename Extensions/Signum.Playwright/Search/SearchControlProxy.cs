using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

public class SearchControlProxy
{
    public ILocator Element { get; private set; }
    public ResultTableProxy Results { get; private set; }

    public async Task<object> QueryNameAsync() => QueryLogic.ToQueryName((await Element.GetAttributeAsync("data-query-key"))!);

    public async Task<FiltersProxy> GetFiltersAsync() => new FiltersProxy(FiltersPanel, await QueryNameAsync());
    public ColumnEditorProxy ColumnEditor() => new ColumnEditorProxy(Element.Locator(".sf-column-editor"));

    public PaginationSelectorProxy Pagination => new PaginationSelectorProxy(this);

    public SearchControlProxy(ILocator element)
    {
        Element = element;
        Results = new ResultTableProxy(Element.Locator(".sf-scroll-table-container"), this);
    }

    public ILocator SearchButton => Element.Locator(".sf-query-button.sf-search");

    public async Task SearchAsync()
    {
        await WaitSearchCompletedAsync(() => SearchButton.ClickAsync());
    }

    public async Task WaitSearchCompletedAsync(Func<Task> searchTrigger)
    {
        var counter = await Element.GetAttributeAsync("data-search-count");
        if (searchTrigger != null)
            await searchTrigger();
        await WaitSearchCompletedAsync(counter);
    }

    public async Task WaitInitialSearchCompletedAsync()
    {
        await WaitSearchCompletedAsync((string?)null);
    }

    private async Task WaitSearchCompletedAsync(string? counter)
    {
        await Element.Page.WaitForFunctionAsync(
            $"() => document.querySelector('{Element}').getAttribute('data-search-count') != '{counter}'"
        );
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

        var filters = await GetFiltersAsync();
        var filterProxy = await filters.GetNewFilterAsync(async () => await menuItem.ClickAsync());

        return (FilterConditionProxy)filterProxy;
    }

    public async Task<FilterConditionProxy> AddQuickFilterAsync(string token)
    {
        await Results.HeaderCellElement(token).ClickAsync(new() { Button = MouseButton.Right });
        var menuItem = ContextualMenu.Locator(".sf-quickfilter-header a");
        await menuItem.WaitForAsync();
        var filters = await GetFiltersAsync();
        var FilterProxy = await filters.GetNewFilterAsync(async () => await menuItem.ClickAsync());
        return (FilterConditionProxy)FilterProxy;
    }

    public async Task<FrameModalProxy<T>> CreateAsync<T>() where T : ModifiableEntity
    {
        await CreateButton.ClickAsync();

        var modalLocator = this.Element.Page.Locator(".sf-selector-modal, .sf-modal");
        await modalLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

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
