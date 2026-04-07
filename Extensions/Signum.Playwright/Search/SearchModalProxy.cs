using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for SearchModal.tsx
/// </summary>
public class SearchModalProxy : ModalProxy
{
    public SearchControlProxy SearchControl { get; private set; } = null!;
    public ResultTableProxy Results => SearchControl.Results;
    public FiltersProxy Filters => SearchControl.Filters;
    public PaginationSelectorProxy Pagination => SearchControl.Pagination;

    SearchModalProxy(ILocator element)
        : base(element)
    {
        
    }

    public static async Task<SearchModalProxy> NewAsync(ILocator element, bool waitInitialSearch = true)
    {
        return new SearchModalProxy(element)
        {
            SearchControl = await SearchControlProxy.NewAsync(element.Locator(".sf-search-control"), waitInitialSearch)
        };
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

    public async Task<ILocator> SelectLiteAsync_Capture(Lite<IEntity> lite, int? subRowIndex = null)
    {
        return await this.Modal.Page.CaptureModalAsync(() => this.SelectLiteAsync(lite, subRowIndex));
    }

    public async Task SelectByPositionAsync(int rowIndex)
    {
        await this.SearchControl.Results.SelectRowAsync(rowIndex);
        await this.OkWaitClosedAsync();
        await DisposeAsync();
    }

    public async Task<ILocator> SelectByPositionAsync_Capture(int rowIndex)
    {
        return await this.Modal.Page.CaptureModalAsync(() => this.SelectByPositionAsync(rowIndex));
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

    public async Task<ILocator> SelectByIdAsync_Capture(PrimaryKey id)
    {
        return await this.Modal.Page.CaptureModalAsync(() => this.SelectByIdAsync(id));
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
    public static async Task<SearchModalProxy> AsSearchModal(this ILocator modal, bool waitInitialSearch = true)
    {
        return await SearchModalProxy.NewAsync(modal, waitInitialSearch);
    }
}
