using Signum.Playwright.Frames;
using Signum.Playwright.LineProxies;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright;

public interface IEntityButtonContainer : ILineContainer
{
    Task<EntityInfoProxy> GetEntityInfoAsync();
    ILocator Container { get; }
}

public interface IEntityButtonContainer<T> : IEntityButtonContainer, ILineContainer<T>
    where T : IModifiableEntity
{
}

public static class EntityButtonContainerExtensions
{
    public static async Task<Lite<T>> GetLiteAsync<T>(this IEntityButtonContainer<T> container) where T : Entity
    {
        var entityInfo = await container.GetEntityInfoAsync();
        return (Lite<T>)entityInfo.ToLite();
    }

    public static ILocator OperationButton(this IEntityButtonContainer container, OperationSymbol symbol, string? groupId = null)
    {
        if (groupId != null)
        {
            var groupButton = container.Element.Locator($"#{groupId}");
            if (groupButton.GetAttributeAsync("aria-expanded").Result != "true")
            {
                groupButton.ClickAsync().GetAwaiter().GetResult();
            }

            return container.Container.Locator($"a[data-operation='{symbol.Key}']");
        }

        return container.Container.Locator($"button[data-operation='{symbol.Key}']");
    }

    public static ILocator OperationButton<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
        where T : Entity
    {
        return container.OperationButton(symbol.Symbol);
    }

    public static async Task<bool> OperationEnabledAsync(this IEntityButtonContainer container, OperationSymbol symbol)
    {
        var button = container.OperationButton(symbol);
        return !await button.IsDisabledAsync();
    }

    public static async Task<bool> OperationEnabledAsync<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
        where T : Entity
    {
        return await container.OperationEnabledAsync(symbol.Symbol);
    }

    public static async Task<bool> OperationDisabledAsync(this IEntityButtonContainer container, OperationSymbol symbol)
    {
        return !await container.OperationEnabledAsync(symbol);
    }

    public static async Task<bool> OperationDisabledAsync<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
        where T : Entity
    {
        return await container.OperationDisabledAsync(symbol.Symbol);
    }

    public static async Task<bool> OperationNotPresentAsync<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
        where T : Entity
    {
        var button = container.OperationButton(symbol);
        return await button.CountAsync() == 0;
    }

    public static async Task OperationClickAsync(this IEntityButtonContainer container, OperationSymbol symbol, string? groupId = null)
    {
        var button = container.OperationButton(symbol, groupId);
        await button.ClickAsync();
    }

    public static async Task OperationClickAsync<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
        where T : Entity
    {
        await container.OperationClickAsync(symbol.Symbol);
    }

    public static async Task<ILocator> OperationClickCaptureAsync(this IEntityButtonContainer container, OperationSymbol symbol, string? groupId = null)
    {
        var button = container.OperationButton(symbol, groupId);
        await button.ClickAsync();
        var modalLocator = container.Page.Locator(".modal-dialog").Last;
        await modalLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        return modalLocator;
    }

    public static async Task<ILocator> OperationClickCaptureAsync<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol, string? groupId = null)
        where T : Entity
    {
        return await container.OperationClickCaptureAsync(symbol.Symbol, groupId);
    }

    public static async Task ExecuteAsync<T>(this IEntityButtonContainer<T> container, ExecuteSymbol<T> symbol, bool consumeAlert = false, bool checkValidationErrors = true)
        where T : Entity
    {
        await container.WaitReloadAsync(async () =>
        {
            await container.OperationClickAsync(symbol);
            if (consumeAlert)
                await container.Page.Locator("div.sf-message-modal button:has-text('Yes')").ClickAsync();
        });

        if (checkValidationErrors && container is IValidationSummaryContainer vs)
        {
            await AssertNoErrorsAsync(vs);
        }
    }

    //TODO
    public static async Task WaitReloadAsync(this IEntityButtonContainer container, Func<Task> action)
    {
        var oldCount = await container.RefreshCountAsync();

        await action();

        await container.Page.WaitForFunctionAsync(
            @"(oldCount) => {
            const el = document.querySelector('div.sf-main-control[data-refresh-count]');
            if (!el) return false;
            return parseInt(el.getAttribute('data-refresh-count')) !== oldCount;
        }",
            oldCount ?? -1
        );
    }

    private static async Task<Task> AssertNoErrorsAsync(IValidationSummaryContainer vs)
    {
        var errors = await vs.ValidationErrorsAsync();
        if (!errors.IsNullOrEmpty())
            throw new InvalidOperationException("Validation Errors found: \n" + errors.ToString("\n").Indent(4));

        return Task.CompletedTask;
    }

    public static async Task DeleteAsync<T>(this FrameModalProxy<T> container, DeleteSymbol<T> symbol, bool consumeAlert = true)
        where T : Entity
    {
        await container.OperationClickAsync(symbol);
        if (consumeAlert)
            await container.Page.Locator("div.sf-message-modal button:has-text('Yes')").ClickAsync();

        await container.WaitForCloseAsync();
    }

    public static async Task<FrameModalProxy<T>> ConstructFromAsync<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
        where T : Entity
        where F : Entity
    {
        var element = await container.OperationClickCaptureAsync(symbol);
        var modal = await new FrameModalProxy<T>(container.Page, element).WaitLoadedAsync();
        return modal;
    }

    public static async Task<FramePageProxy<T>> ConstructFromNormalPageAsync<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
        where T : Entity
        where F : Entity
    {
        await container.OperationClickAsync(symbol);

        await container.Page.WaitForFunctionAsync(@"(container) => {
            try { return container.EntityInfo().IsNew; } catch { return false; }
        }", container);

        return new FramePageProxy<T>(container.Page);
    }

    public static async Task<long?> RefreshCountAsync(this IEntityButtonContainer container)
    {
        try
        {
            var elem = container.Element.Locator("div.sf-main-control[data-refresh-count]");
            if (await elem.CountAsync() == 0)
                return null;

            var value = await elem.GetAttributeAsync("data-refresh-count");
            return long.TryParse(value, out var result) ? result : null;
        }
        catch
        {
            return null;
        }
    }
}
