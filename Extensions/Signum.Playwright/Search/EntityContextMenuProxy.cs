using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for ContextMenu in SearchControlLoaded.tsx
/// </summary>
public class EntityContextMenuProxy
{
    public ResultTableProxy ResultTable { get; private set; }
    public ILocator Element { get; private set; }

    public List<Lite<IEntity>> SelectedEntities; 

    public EntityContextMenuProxy(ResultTableProxy resultTable, ILocator element, List<Lite<IEntity>> selectedEntities)
    {
        ResultTable = resultTable;
        Element = element;
        SelectedEntities = selectedEntities;
    }

    public ILocator QuickLink(string name)
    {
        return Element.Locator($"a[data-name='{name}']");
    }

    public async Task<SearchModalProxy> QuickLinkClickSearchAsync(string name)
    {
        var a = QuickLink(name);
        await a.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var popup = await a.CaptureOnClickAsync();
        return await SearchModalProxy.NewAsync(popup);
    }

    public async Task ExecuteAsync<T>(
        ExecuteSymbol<T> executeSymbol,
        bool consumeConfirmation = false,
        bool shouldDisappear = false,
        Func<EntityContextMenuProxy, Func<Task>>? customCheck = null,
        bool scrollTo = false
    ) where T : Entity
    {
        var check = customCheck != null ? customCheck(this) : this.GetShouldDisappearCheckAsync(shouldDisappear);

        var op = Operation(executeSymbol);

        if (scrollTo)
            await op.ScrollIntoViewIfNeededAsync();

        if (consumeConfirmation)
        {
            await using (var mm = await this.Element.Page.GetMessageModalAsync())
            {
                if (mm != null)
                    await mm.OkWaitClosedAsync();
            }
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
        ) 
        where F : Entity
        where T : Entity
    {
        var check = customCheck != null ? customCheck(this) : GetShouldDisappearCheckAsync(shouldDisappear);

        var modalLocator = Operation(constructSymbol);
        if (scrollTo)
            await modalLocator.ScrollIntoViewIfNeededAsync();
        var modal = await modalLocator.CaptureOnClickAsync();

        var result = await FrameModalProxy<T>.NewAsync(modal);
        result.Disposing += okPressed => check();

        return result;
    }

    public async Task<FrameModalProxy<T>> ConstructFromManyAsync<F, T>(
        ConstructSymbol<T>.FromMany<F> constructSymbol,
        bool shouldDisappear = false,
        Func<EntityContextMenuProxy, Func<Task>>? customCheck = null,
        bool scrollTo = false
        )
        where F : Entity
        where T : Entity
    {
        var check = customCheck != null ? customCheck(this) : GetShouldDisappearCheckAsync(shouldDisappear);

        var modalLocator = Operation(constructSymbol);
        if (scrollTo)
            await modalLocator.ScrollIntoViewIfNeededAsync();
        var modal = await modalLocator.CaptureOnClickAsync();

        var result = await FrameModalProxy<T>.NewAsync(modal);
        result.Disposing += okPressed => check();

        return result;
    }

    public async Task DeleteAsync(
        IOperationSymbolContainer symbolContainer,
        bool consumeConfirmation = true,
        bool shouldDisappear = true,
        Func<EntityContextMenuProxy, Func<Task>>? customCheck = null,
        bool scrollTo = false)
    {
        var check = customCheck != null ? customCheck(this) : this.GetShouldDisappearCheckAsync(shouldDisappear);

        var op = Operation(symbolContainer);
        if (scrollTo)
            await op.ScrollIntoViewIfNeededAsync();

        if (consumeConfirmation)
        {
            await using (var mm = await this.Element.Page.GetMessageModalAsync())
            {
                if (mm != null)
                    await mm.OkWaitClosedAsync();
            }
        }
        else
        {
            await op.ClickAsync();
        }

        await check();
    }

    private Func<Task> GetShouldDisappearCheckAsync(bool shouldDisappear)
    {
        if (shouldDisappear)
            return () => ResultTable.WaitNoVisibleAsync(this.SelectedEntities);

        return () => ResultTable.WaitSuccessAsync(this.SelectedEntities);
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
        return await op.CaptureOnClickAsync();
    }

    public async Task WaitNotLoadingAsync()
    {
        await Element.Locator("li.sf-tm-selected-loading").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }
}
