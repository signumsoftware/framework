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


    public static async Task<ILocator> OperationButtonAsync(this IEntityButtonContainer container, OperationSymbol symbol, string? groupId = null)
    {
        if (groupId != null)
        {
            var groupButton = container.Element.Locator($"#{groupId}");
            if (groupButton.GetAttributeAsync("aria-expanded").Result != "true")
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

    public static async Task ExecuteAsync<T>(this IEntityButtonContainer<T> container, ExecuteSymbol<T> symbol, bool consumeAlert = false, bool checkValidationErrors = true)
        where T : Entity
    {
        await container.WaitReloadAsync(async () =>
        {
            await container.OperationClickAsync(symbol);
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

    public static async Task DeleteAsync<T>(this FrameModalProxy<T> container, DeleteSymbol<T> symbol, bool consumeAlert = true)
        where T : Entity
    {
        await container.OperationClickAsync(symbol);
        if (consumeAlert)
            await container.Modal.Page.Locator("div.sf-message-modal button:has-text('Yes')").ClickAsync();

        await container.WaitForCloseAsync();
    }

    public static async Task<FrameModalProxy<T>> ConstructFromAsync<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
        where T : Entity
        where F : Entity
    {
        var element = await container.OperationClickCaptureAsync(symbol);
        var modal = await new FrameModalProxy<T>(element).WaitLoadedAsync();
        return modal;
    }

    public static async Task<FramePageProxy<T>> ConstructFromNormalPageAsync<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
        where T : Entity
        where F : Entity
    {
        await container.OperationClickAsync(symbol);

        await container.Container.Page.WaitAsync(async () =>
        {
            try { return (await container.GetEntityInfoAsync()).IsNew; } catch { return false; }
        });

        return await FramePageProxy<T>.CreateAsync(container.Container.Page);
    }

    public static async Task<long?> RefreshCountAsync(this IEntityButtonContainer container)
    {
        var value = await container.Element.Locator("div.sf-main-control").GetAttributeAsync("data-refresh-count");
        return long.TryParse(value, out var result) ? result : null;
    }

    public static async Task WaitReloadAsync(this IEntityButtonContainer container, Func<Task> action)
    {
        var oldCount = await container.RefreshCountAsync();

        await action();

        await Assertions.Expect(container.Container.Locator("div.sf-main-control")).Not.ToHaveAttributeAsync("data-refresh-count", oldCount?.ToString() ?? "");
    }
}
