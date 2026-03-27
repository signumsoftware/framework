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

    public SearchModalProxy(ILocator element)
        : base(element)
    {
        this.SearchControl = new SearchControlProxy(element.Locator(".sf-search-control"));
    }

    public async Task Initialize( bool waitInitialSearch)
    {
        await SearchControl.Initialize();

        if(waitInitialSearch)
            await SearchControl.WaitInitialSearchCompletedAsync();
    }

    public async Task SelectLiteAsync(Lite<IEntity> lite)
    {
        if (!await this.SearchControl.FiltersVisibleAsync())
            await this.SearchControl.ToggleFiltersAsync(true);

        await this.SearchControl.Filters.AddFilterAsync("Entity.Id", FilterOperation.EqualTo, lite.Id);

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
        await SearchControl.CreateButton.ClickAsync();

        var modalLocator = Modal.Locator(".sf-modal");
        await modalLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        var modal = await FrameModalProxy<T>.NewAsync(modalLocator);
        await action(modal);
        var message = modalLocator.Locator(".message-modal");
        if (await message.CountAsync() > 0)
        {
            var msg = new MessageModalProxy(message);
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
    public static async Task<SearchModalProxy> Await_AsSearchModal(this Task<ILocator> modal, bool waitInitialSearch = true)
    {
        var result = new SearchModalProxy(await modal);
        await result.Initialize(waitInitialSearch);
        return result;
    }

    public static async Task<SearchModalProxy> AsSearchModal(this ILocator modal, bool waitInitialSearch = true)
    {
         var result = new SearchModalProxy(modal);
        await result.Initialize(waitInitialSearch);
        return result;
    }
}
