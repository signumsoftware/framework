using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

public class SearchModalProxy : ModalProxy
{
    public SearchControlProxy SearchControl { get; private set; }
    public ResultTableProxy Results => SearchControl.Results;
    public Task<FiltersProxy> GetFiltersAsync() => SearchControl.GetFiltersAsync();
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

        var filters = await this.SearchControl.GetFiltersAsync();
        await filters.AddFilterAsync("Entity.Id", FilterOperation.EqualTo, lite.Id);

        await this.SearchControl.SearchAsync();

        await this.SearchControl.Results.SelectRowAsync(lite);

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

        var filters = await this.SearchControl.GetFiltersAsync();
        await filters.AddFilterAsync("Entity.Id", FilterOperation.EqualTo, id);
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
        await SearchControl.CreateButton.ClickAsync();

        var modalLocator = Page.Locator(".sf-modal");
        await modalLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        var modal = new FrameModalProxy<T>(Page, modalLocator);
        await action(modal);
        var message = Page.Locator(".message-modal");
        if (await message.CountAsync() > 0)
        {
            var msg = new MessageModalProxy(message, Page);
            await msg.ClickWaitCloseAsync(MessageModalButton.Yes);
        }

        await modalLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
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
