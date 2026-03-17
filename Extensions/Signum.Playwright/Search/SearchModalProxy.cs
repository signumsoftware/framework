using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

public class SearchModalProxy : ModalProxy
{
    public SearchControlProxy SearchControl { get; private set; }
    public ResultTableProxy Results => SearchControl.Results;
    public FiltersProxy Filters => SearchControl.Filters;
    public PaginationSelectorProxy Pagination => SearchControl.Pagination;

    public SearchModalProxy(ILocator element, IPage page, bool waitInitialSearch = true)
        : base(element, page)
    {
        this.SearchControl = new SearchControlProxy(element.Locator(".sf-search-control"), page);

        if (waitInitialSearch)
            this.SearchControl.WaitInitialSearchCompletedAsync().GetAwaiter().GetResult();
    }

    public async Task SelectLiteAsync(Lite<IEntity> lite)
    {
        if (!await this.SearchControl.FiltersVisibleAsync())
            await this.SearchControl.ToggleFiltersAsync(true);

        await this.SearchControl.Filters.AddFilterAsync("Entity.Id", FilterOperation.EqualTo, lite.Id);

        await this.SearchControl.SearchAsync();

        this.SearchControl.Results.SelectRow(lite);

        await this.OkWaitClosedAsync();

        await DisposeAsync();
    }

    public async Task SelectByPositionAsync(int rowIndex)
    {
        this.SearchControl.Results.SelectRow(rowIndex);
        await this.OkWaitClosedAsync();
        await DisposeAsync();
    }

    public async Task SelectByPositionOrderByIdAsync(int rowIndex)
    {
        this.Results.OrderBy("Id");
        this.SearchControl.Results.SelectRow(rowIndex);
        await this.OkWaitClosedAsync();
        await DisposeAsync();
    }

    public async Task SelectByIdAsync(PrimaryKey id)
    {
        if (!await this.SearchControl.FiltersVisibleAsync())
            await this.SearchControl.ToggleFiltersAsync(true);

        await this.SearchControl.Filters.AddFilterAsync("Entity.Id", FilterOperation.EqualTo, id);
        await this.SearchControl.SearchAsync();

        this.Results.SelectRow(0);

        await this.OkWaitClosedAsync();
        await DisposeAsync();
    }

    public async Task SelectByPositionAsync(params int[] rowIndexes)
    {
        await this.SearchControl.SearchAsync();

        foreach (var index in rowIndexes)
            this.SearchControl.Results.SelectRow(index);

        await this.OkWaitClosedAsync();
        await DisposeAsync();
    }

    public async Task<FrameModalProxy<T>> CreateAsync<T>() where T : ModifiableEntity
    {
        return await SearchControl.CreateAsync<T>();
    }

    public async Task CreateAndSelectAsync<T>(Func<FrameModalProxy<T>, Task> action) where T : ModifiableEntity
    {
        var message = await this.Element.CapturePopupAsync(async () =>
        {
            var modal = await SearchControl.CreateAsync<T>();
            await action(modal);
        });

        await message.AsMessageModalAsync().ClickWaitCloseAsync(MessageModalButton.Yes);

        await WaitNotVisibleAsync();
    }

    public async Task SearchAsync()
    {
        await this.SearchControl.SearchAsync();
    }
}

public static class SearchModalExtensions
{
    public static SearchModalProxy AsSearchModal(this ILocator modal, IPage page, bool waitInitialSearch = true)
    {
        return new SearchModalProxy(modal, page, waitInitialSearch);
    }
}
