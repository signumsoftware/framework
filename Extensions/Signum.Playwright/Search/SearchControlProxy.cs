using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

public class SearchControlProxy
{
    public IPage Page { get; private set; }
    public ILocator Element { get; private set; }
    public ResultTableProxy Results { get; private set; }

    public object QueryName => QueryLogic.ToQueryName(Element.GetAttributeAsync("data-query-key").GetAwaiter().GetResult());

    public FiltersProxy Filters => new FiltersProxy(FiltersPanel, QueryName, Page);
    public ColumnEditorProxy ColumnEditor() => new ColumnEditorProxy(Element.Locator(".sf-column-editor"));

    public PaginationSelectorProxy Pagination => new PaginationSelectorProxy(this);

    public SearchControlProxy(ILocator element, IPage page)
    {
        Page = page;
        Element = element;
        Results = new ResultTableProxy(Element.Locator(".sf-scroll-table-container"), this, Page);
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
        await Page.WaitForFunctionAsync(
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

    public ILocator ToggleFiltersButton => Element.Locator(".sf-filter-button");
    public ILocator FiltersPanel => Element.Locator(".sf-filters-list");

    public async Task ToggleFiltersAsync(bool show)
    {
        await ToggleFiltersButton.ClickAsync();
        if (show)
            await FiltersPanel.WaitForAsync();
        else
            await FiltersPanel.WaitForElementStateAsync(ElementState.Hidden);
    }

    public ILocator ContextualMenu => Page.Locator(".sf-context-menu");

    public async Task<FilterConditionProxy> AddQuickFilterAsync(int rowIndex, string token)
    {
        await Results.CellElement(rowIndex, token).ClickAsync(new() { Button = MouseButton.Right });
        var menuItem = ContextualMenu.Locator(".sf-quickfilter-header a");
        await menuItem.WaitForAsync();
        var FilterProxy = await Filters.GetNewFilterAsync(async () => await menuItem.ClickAsync());
        return (FilterConditionProxy)FilterProxy;
    }

    public async Task<FilterConditionProxy> AddQuickFilterAsync(string token)
    {
        await Results.HeaderCellElement(token).ClickAsync(new() { Button = MouseButton.Right });
        var menuItem = ContextualMenu.Locator(".sf-quickfilter-header a");
        await menuItem.WaitForAsync();
        var FilterProxy = await Filters.GetNewFilterAsync(async () => await menuItem.ClickAsync());
        return (FilterConditionProxy)FilterProxy;
    }

    public async Task<FrameModalProxy<T>> CreateAsync<T>() where T : ModifiableEntity
    {
        var popup = await CreateButton.ClickAsync();
        var modal = await SelectorModalProxy.IsSelectorAsync(popup);
        if (modal)
            popup = await popup.AsSelectorModal().SelectAndCapture<T>();
        return new FrameModalProxy<T>(popup).WaitLoaded();
    }

    public ILocator CreateButton => Element.Locator(".sf-create");

    public async Task<bool> HasMultiplyMessageAsync() => await Element.Locator(".sf-td-multiply").CountAsync() > 0;
    public async Task<bool> FiltersVisibleAsync() => await FiltersPanel.IsVisibleAsync();

    public ILineContainer<T> SimpleFilterBuilder<T>() where T : ModifiableEntity
    {
        return new LineContainer<T>(Element.Locator(".simple-filter-builder"), Page);
    }
}
