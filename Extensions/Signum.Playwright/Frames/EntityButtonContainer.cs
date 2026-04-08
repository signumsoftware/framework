using Signum.Playwright.Frames;
using Signum.Playwright.LineProxies;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright;

public interface IEntityButtonContainer : ILineContainer
{
    Task<EntityInfoProxy> GetEntityInfoAsync();
    ILocator Container { get; }
    ILocator MainControl { get; }
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


    public static async Task<ILocator> OperationButtonAsync(this IEntityButtonContainer container, OperationSymbol symbol, string? groupId = null)
    {
        if (groupId != null)
        {
            var groupButton = container.Container.Locator($"#{groupId}");
            if ((await groupButton.GetAttributeAsync("aria-expanded")) != "true")
            {
                await groupButton.ClickAsync();
            }

            return container.Container.Locator($"a[data-operation='{symbol.Key}']");
        }

        return container.Container.Locator($"button[data-operation='{symbol.Key}']");
    }

    public static Task<ILocator> OperationButtonWaitVisibleAsync<T>(this IEntityButtonContainer container, IEntityOperationSymbolContainer<T> symbol, string? groupId = null)
        where T : Entity
    {
        return OperationButtonWaitVisibleAsync(container, symbol.Symbol, groupId);
    }

    public static async Task<ILocator> OperationButtonWaitVisibleAsync(this IEntityButtonContainer container, OperationSymbol symbol, string? groupId = null)
    {
        if (groupId != null)
        {
            var groupButton = await container.Element.Locator($"#{groupId}").WaitVisibleAsync();
            if (groupButton.GetAttributeAsync("aria-expanded").Result != "true")
            {
                await groupButton.ClickAsync();
            }

            return await container.Container.Locator($"a[data-operation='{symbol.Key}']").WaitVisibleAsync();
        }

        return await container.Container.Locator($"button[data-operation='{symbol.Key}']").WaitVisibleAsync();
    }

    public static Task<ILocator> OperationButtonAsync<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol, string? groupId = null)
        where T : Entity
    {
        return container.OperationButtonAsync(symbol.Symbol, groupId);
    }

    public static async Task<bool> OperationEnabledAsync(this IEntityButtonContainer container, OperationSymbol symbol, string? groupId = null)
    {
        var button = await container.OperationButtonAsync(symbol, groupId);
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
        var button = await container.OperationButtonAsync(symbol);
        return await button.CountAsync() == 0;
    }

    public static async Task OperationClickAsync(this IEntityButtonContainer container, OperationSymbol symbol, string? groupId = null)
    {
        var button = await container.OperationButtonAsync(symbol, groupId);
        await button.ClickAsync();
    }

    public static async Task OperationClickAsync<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol, string? groupId = null)
        where T : Entity
    {
        await container.OperationClickAsync(symbol.Symbol, groupId);
    }

    public static async Task<ILocator> OperationClickCaptureAsync(this IEntityButtonContainer container, OperationSymbol symbol, string? groupId = null)
    {
        var button = await container.OperationButtonAsync(symbol, groupId);
        return await button.CaptureOnClickAsync();
    }

    public static async Task<ILocator> OperationClickCaptureAsync<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol, string? groupId = null)
        where T : Entity
    {
        return await container.OperationClickCaptureAsync(symbol.Symbol, groupId);
    }

    public static async Task ExecuteAsync<T>(this IEntityButtonContainer<T> container, ExecuteSymbol<T> symbol, bool consumeAlert = false, bool checkValidationErrors = true, string? groupId = null)
        where T : Entity
    {
        await container.WaitReloadAsync(async () =>
        {
            await container.OperationClickAsync(symbol, groupId);
            if (consumeAlert)
                await container.Container.Page.CloseMessageModalAsync(MessageModalButton.Yes);
        });

        if (checkValidationErrors && container is IValidationSummaryContainer vs)
        {
            await AssertNoErrorsAsync(vs);
        }
    }

    //TODO
  

    private static async Task<Task> AssertNoErrorsAsync(IValidationSummaryContainer vs)
    {
        var errors = await vs.ValidationErrorsAsync();
        if (!errors.IsNullOrEmpty())
            throw new InvalidOperationException("Validation Errors found: \n" + errors.ToString("\n").Indent(4));

        return Task.CompletedTask;
    }

    public static async Task DeleteAsync<T>(this FrameModalProxy<T> container, DeleteSymbol<T> symbol, bool consumeAlert = true, string? groupId = null)
        where T : Entity
    {
        await container.OperationClickAsync(symbol, groupId);
        if (consumeAlert)
            await container.Modal.Page.CloseMessageModalAsync(MessageModalButton.Yes);

        await container.WaitForCloseAsync();
    }

    public static async Task<FrameModalProxy<T>> ConstructFromAsync<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol, string? groupId = null)
        where T : Entity
        where F : Entity
    {
        var element = await container.OperationClickCaptureAsync(symbol, groupId);
        var modal = await FrameModalProxy<T>.NewAsync(element);
        return modal;
    }

    public static async Task<FramePageProxy<T>> ConstructFromFramePageAsync<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol, string? groupId = null)
        where T : Entity
        where F : Entity
    {
        await container.OperationClickAsync(symbol, groupId);

        await container.Container.Page.WaitAsync(async () =>
        {
            try { return (await container.GetEntityInfoAsync()).IsNew; } catch { return false; }
        });

        return await FramePageProxy<T>.NewAsync(container.Container.Page);
    }

    public static async Task<long?> RefreshCountAsync(this IEntityButtonContainer container)
    {
        var value = await container.MainControl.GetAttributeAsync("data-refresh-count");
        return long.TryParse(value, out var result) ? result : null;
    }

    public static async Task WaitReloadAsync(this IEntityButtonContainer container, Func<Task> action)
    {
        var oldCount = await container.RefreshCountAsync();

        await action();

        await container.MainControl.WaitAttributeAsync("data-refresh-count", oldCount?.ToString(), "!==");
    }
}
