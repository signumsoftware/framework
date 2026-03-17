using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

public class SearchPageProxy : IAsyncDisposable
{
    public IPage Page { get; private set; }
    public SearchControlProxy SearchControl { get; private set; }
    public ResultTableProxy Results => SearchControl.Results;
    public FiltersProxy Filters => SearchControl.Filters;
    public PaginationSelectorProxy Pagination => SearchControl.Pagination;

    public SearchPageProxy(IPage page)
    {
        Page = page;
    }

    public async Task InitializeAsync()
    {
        var element = Page.Locator(".sf-search-page .sf-search-control");
        await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        SearchControl = new SearchControlProxy(element, Page);
    }

    public async Task<FrameModalProxy<T>> CreateAsync<T>() where T : ModifiableEntity
    {
        var createButton = SearchControl.CreateButton;
        var popup = await createButton.ClickCaptureAsync();

        if (await SelectorModalProxy.IsSelectorAsync(popup))
            popup = await popup.AsSelectorModal().SelectAndCaptureAsync<T>();

        return new FrameModalProxy<T>(popup);
    }

    public async Task<FramePageProxy<T>> CreateInPlaceAsync<T>() where T : ModifiableEntity
    {
        await SearchControl.CreateButton.ClickAsync();
        return new FramePageProxy<T>(Page);
    }

    public async Task<FramePageProxy<T>> CreateInTabAsync<T>() where T : ModifiableEntity
    {
        var oldPages = Page.Context.Pages.ToList();

        await SearchControl.CreateButton.ClickAsync();

        // Warten auf neues Tab
        await Page.Context.WaitForEventAsync(ContextEvent.Page);

        var newPages = Page.Context.Pages.ToList();
        var newPage = newPages.Except(oldPages).FirstOrDefault();

        if (newPage == null)
            throw new InvalidOperationException("Neues Tab konnte nicht gefunden werden.");

        var result = new FramePageProxy<T>(newPage);
        result.OnDisposed += () => newPage.CloseAsync();
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
        // Bei Playwright meist Page nicht direkt schließen, nur Ressourcen freigeben falls nötig
        // await Page.CloseAsync();
    }
}
