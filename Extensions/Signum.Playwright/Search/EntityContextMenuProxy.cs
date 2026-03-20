using Microsoft.Playwright;
using Signum.Playwright.Frames;

namespace Signum.Playwright.Search;

public class EntityContextMenuProxy
{
    public ResultTableProxy ResultTable { get; private set; }
    public ILocator Element { get; private set; }

    public IPage Page => ResultTable.Page;

    public EntityContextMenuProxy(ResultTableProxy resultTable, ILocator element)
    {
        ResultTable = resultTable;
        Element = element;
    }

    public ILocator QuickLink(string name)
    {
        return Element.Locator($"a[data-name='{name}']");
    }

    public async Task<SearchModalProxy> QuickLinkClickSearchAsync(string name)
    {
        var a = QuickLink(name);
        await a.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var popup = await a.CaptureOnClickAsync(Page);
        return new SearchModalProxy(popup, Page);
    }

    public async Task ExecuteClickAsync<T>(
        ExecuteSymbol<T> executeSymbol,
        bool consumeConfirmation = false,
        bool shouldDisappear = false,
        Func<EntityContextMenuProxy, Func<Task>>? customCheck = null,
        bool scrollTo = false
    ) where T : Entity
    {
        var check = customCheck != null ? customCheck(this) : GetShouldDisappearCheckAsync(shouldDisappear);

        var op = Operation(executeSymbol);

        if (scrollTo)
            await op.ScrollIntoViewIfNeededAsync();

        if (consumeConfirmation)
        {
            await ResultTable.Page.RunAndConsumeAlertAsync(async () =>
            {
                await op.ClickAsync();
            });
        }
        else
        {
            await op.ClickAsync();
        }

        await check();
    }

    public async Task<FrameModalProxy<T>> ConstructFromAsync<F, T>(
        ConstructSymbol<T>.From<F> constructSymbol,
        bool shouldDisappear = false,
        Func<EntityContextMenuProxy, Func<Task>>? customCheck = null,
        bool scrollTo = false
    ) where F : Entity
      where T : Entity
    {
        var check = customCheck != null ? customCheck(this) : GetShouldDisappearCheckAsync(shouldDisappear);

        var modalLocator = Operation(constructSymbol);
        if (scrollTo)
            await modalLocator.ScrollIntoViewIfNeededAsync();
        var modal = await modalLocator.CaptureOnClickAsync(Page);

        var result = new FrameModalProxy<T>(Page, modal);
        result.Disposing += async okPressed => await check();

        return result;
    }

    public async Task DeleteClickAsync(
    IOperationSymbolContainer symbolContainer,
    bool consumeConfirmation = true,
    bool shouldDisappear = true,
    Func<EntityContextMenuProxy, Func<Task>>? customCheck = null,
    bool scrollTo = false)
    {
        var check = customCheck != null ? customCheck(this) : GetShouldDisappearCheckAsync(shouldDisappear);

        var op = Operation(symbolContainer);
        if (scrollTo)
            await op.ScrollIntoViewIfNeededAsync();

        if (consumeConfirmation)
        {
            await ResultTable.Page.RunAndConsumeAlertAsync(async () =>
            {
                await op.ClickAsync();
            });
        }
        else
        {
            await op.ClickAsync();
        }

        await check();
    }

    private Func<Task> GetShouldDisappearCheckAsync(bool shouldDisappear)
    {
        var selectedEntities = ResultTable.SelectedEntitiesAsync();
        return async () =>
        {
            if (shouldDisappear)
                await ResultTable.WaitNoVisibleAsync(await selectedEntities);
            else
                await ResultTable.WaitSuccessAsync(await selectedEntities);
        };
    }

    public ILocator Operation(IOperationSymbolContainer symbolContainer)
    {
        return Element.Locator($"a[data-operation='{symbolContainer.Symbol.Key}']");
    }

    public async Task<bool> OperationIsDisabledAsync(IOperationSymbolContainer symbolContainer)
    {
        var op = Operation(symbolContainer);
        await op.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        return await op.IsDisabledAsync();
    }

    public async Task<ILocator> OperationClickCaptureAsync(IOperationSymbolContainer symbolContainer, bool scrollTo = false)
    {
        var op = Operation(symbolContainer);
        if (scrollTo)
            await op.ScrollIntoViewIfNeededAsync();
        return await op.CaptureOnClickAsync(Page);
    }

    public async Task WaitNotLoadingAsync()
    {
        await Element.Locator("li.sf-tm-selected-loading").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }
}
