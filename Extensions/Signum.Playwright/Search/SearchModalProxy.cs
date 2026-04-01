using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for SearchModal.tsx
/// </summary>
public class SearchModalProxy : ModalProxy
{
    public SearchControlProxy SearchControl { get; private set; }
    public ResultTableProxy Results => SearchControl.Results;
    public FiltersProxy Filters => SearchControl.Filters;
    public PaginationSelectorProxy Pagination => SearchControl.Pagination;

    SearchModalProxy(ILocator element)
        : base(element)
    {
        this.SearchControl = new SearchControlProxy(element.Locator(".sf-search-control"));
    }

    public static async Task<SearchModalProxy> NewAsync(ILocator element, bool waitInitialSearch = true)
    {
        var result = new SearchModalProxy(element);
        await result.Initialize(waitInitialSearch);
        return result;
    }

    public async Task Initialize(bool waitInitialSearch)
    {
        await SearchControl.Initialize();

        if(waitInitialSearch)
            await SearchControl.WaitInitialSearchCompletedAsync();
    }

    public async Task SelectLiteAsync(Lite<IEntity> lite, int? subRowIndex = null)
    {
        if (!await this.SearchControl.FiltersVisibleAsync())
            await this.SearchControl.ToggleFiltersAsync(true);

        await this.SearchControl.Filters.AddFilterAsync("Entity.Id", FilterOperation.EqualTo, lite.Id);

        await this.SearchControl.SearchAsync();

        await this.SearchControl.Results.SelectRowAsync(lite, subRowIndex);

        await this.OkWaitClosedAsync();

        await DisposeAsync();
    }

    public async Task SelectByPositionAsync(int rowIndex)
    {
        await this.SearchControl.Results.SelectRowAsync(rowIndex);
        await this.OkWaitClosedAsync();
        await DisposeAsync();
    }

    public async Task SelectByPositionOrderByIdAsync(int rowIndex)
    {
        await this.Results.OrderByAsync("Id");
        await this.SearchControl.Results.SelectRowAsync(rowIndex);
        await this.OkWaitClosedAsync();
        await DisposeAsync();
    }

    public async Task SelectByIdAsync(PrimaryKey id)
    {
        if (!await this.SearchControl.FiltersVisibleAsync())
            await this.SearchControl.ToggleFiltersAsync(true);

        await this.SearchControl.Filters.AddFilterAsync("Entity.Id", FilterOperation.EqualTo, id);
        await this.SearchControl.SearchAsync();

        await this.Results.SelectRowAsync(0);

        await this.OkWaitClosedAsync();
        await DisposeAsync();
    }

    public async Task SelectByPositionAsync(params int[] rowIndexes)
    {
        await this.SearchControl.SearchAsync();

        foreach (var index in rowIndexes)
            await this.SearchControl.Results.SelectRowAsync(index);

        await this.OkWaitClosedAsync();
        await DisposeAsync();
    }

    public async Task<FrameModalProxy<T>> CreateAsync<T>() where T : ModifiableEntity
    {
        return await SearchControl.CreateAsync<T>();
    }

    public async Task CreateAndSelectAsync<T>(Func<FrameModalProxy<T>, Task> action) where T : ModifiableEntity
    {
        var frameModal = await CreateAsync<T>();

        await action(frameModal);
        await this.Modal.Page.CloseMessageModalAsync(MessageModalButton.Yes);

        await this.WaitForCloseAsync();
    }

    public async Task SearchAsync()
    {
        await this.SearchControl.SearchAsync();
    }
}

public static class SearchModalExtensions
{
    public static async Task<SearchModalProxy> Await_AsSearchModal(this Task<ILocator> modal, bool waitInitialSearch = true)
    {
        return await SearchModalProxy.NewAsync(await modal, waitInitialSearch);
    }

    public static async Task<SearchModalProxy> AsSearchModal(this ILocator modal, bool waitInitialSearch = true)
    {
        return await SearchModalProxy.NewAsync(modal, waitInitialSearch);
    }
}
