using Microsoft.Playwright;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Frames;

public class FramePageProxy<T> : ILineContainer<T>, IEntityButtonContainer<T>, IWidgetContainer, IValidationSummaryContainer, IAsyncDisposable 
    where T : ModifiableEntity
{
    public IPage Page { get; }
    public ILocator Element { get; }
    public PropertyRoute Route { get; }

    public FramePageProxy(IPage page)
    {
        Page = page;
        Element = page.Locator(".normal-control");
        Route = PropertyRoute.Root(typeof(T));
    }

    public async Task<FramePageProxy<T>> WaitLoadedAsync()
    {
        await Page.WaitForSelectorAsync(".normal-control");
        
        var error = await Page.GetErrorModalAsync();
        if (error != null)
        {
            await error.ThrowErrorModalAsync();
        }

        await MainControl.WaitVisibleAsync();
        return this;
    }

    public ILocator MainControl => Element.Locator(".sf-main-control");

    public async Task<EntityInfoProxy> GetEntityInfoAsync()
    {
        var attr = await MainControl.First.GetAttributeAsync("data-main-entity");
        if (attr == null)
            throw new InvalidOperationException("data-main-entity attribute not found");

        return EntityInfoProxy.Parse(attr)!;
    }

    public async Task<T> RetrieveEntityAsync()
    {
        var lite = (await GetEntityInfoAsync()).ToLite();
        return (T)(IEntity)lite.RetrieveAndRemember();
    }

    public Action? OnDisposed { get; set; }

    public async ValueTask DisposeAsync()
    {
        OnDisposed?.Invoke();
        await Task.CompletedTask;
    }
}
