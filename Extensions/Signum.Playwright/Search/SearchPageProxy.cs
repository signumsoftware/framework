using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for SearchPage.tsx
/// </summary>
public class SearchPageProxy : IDisposable, IAsyncDisposable
{
    public IPage Page { get; private set; }
    public SearchControlProxy SearchControl { get; private set; } = null!;
    public ResultTableProxy Results => SearchControl.Results;
    public FiltersProxy Filters => SearchControl.Filters;
    public PaginationSelectorProxy Pagination => SearchControl.Pagination;

    SearchPageProxy(IPage page)
    {
        Page = page;
    }

    public static async Task<SearchPageProxy> NewAsync(IPage page, bool waitInitialSearch = true)
    {
        var sp = new SearchPageProxy(page);
        var element = page.Locator(".sf-search-page .sf-search-control");
        await element.WaitVisibleAsync();
        sp.SearchControl = await SearchControlProxy.NewAsync(element, waitInitialSearch);
        return sp;
    }

    public async Task<FrameModalProxy<T>> CreateAsync<T>() where T : ModifiableEntity
    {
        var createButton = SearchControl.CreateButton;
        var popup = await createButton.CaptureOnClickAsync();

        if (await SelectorModalProxy.IsSelectorAsync(popup))
            popup = await popup.AsSelectorModal().SelectAndCaptureAsync<T>();

        return await FrameModalProxy<T>.NewAsync(popup);
    }

    public async Task<FramePageProxy<T>> CreateInPlaceAsync<T>() where T : ModifiableEntity
    {
        await SearchControl.CreateButton.ClickAsync();
        return await FramePageProxy<T>.NewAsync(Page);
    }

    public async Task<FramePageProxy<T>> CreateInTabAsync<T>() where T : ModifiableEntity
    {
        var oldPages = Page.Context.Pages.ToList();

        await SearchControl.CreateButton.ClickAsync();

        var tcs = new TaskCompletionSource<IPage>();
        void Handler(object? sender, IPage page)
        {
            if (!oldPages.Contains(page))
            {
                tcs.TrySetResult(page);
            }
        }

        Page.Context.Page += Handler;
        var newPage = await tcs.Task;
        Page.Context.Page -= Handler;

        if (newPage == null)
            throw new InvalidOperationException("Neues Tab konnte nicht gefunden werden.");

        var result = await FramePageProxy<T>.NewAsync(newPage);

        result.OnDisposed += async () => await newPage.CloseAsync();
        return result;
    }

    public async Task SearchAsync()
    {
        await SearchControl.SearchAsync();
    }

    public async Task WaitLoadedAsync()
    {
        await SearchControl.SearchButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public async ValueTask DisposeAsync()
    {
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}
