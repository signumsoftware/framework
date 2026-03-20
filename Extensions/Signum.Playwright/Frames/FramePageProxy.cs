using Microsoft.Playwright;
using Signum.Playwright.LineProxies;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Frames;

public class FramePageProxy<T> : ILineContainer<T>, IEntityButtonContainer, IWidgetContainer, IValidationSummaryContainer, IAsyncDisposable 
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
        WaitLoadedAsync().GetAwaiter();
    }


    public ILocator Container => Element;

    public Action? OnDisposed { get; set; }

    public async ValueTask DisposeAsync()
    {
        OnDisposed?.Invoke();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public ILocator MainControl => Element.Locator(".sf-main-control");

    async Task<EntityInfoProxy> GetEntityInfoAsync()
    {
        var attr = await MainControl.First.GetAttributeAsync("data-main-entity");
        return attr == null ? throw new InvalidOperationException("data-main-entity attribute not found") : EntityInfoProxy.Parse(attr)!;
    }
    Task<EntityInfoProxy> IEntityButtonContainer.GetEntityInfoAsync()
    {
        return GetEntityInfoAsync();
    }

    public async Task<T> RetrieveEntityAsync()
    {
        var lite = (await GetEntityInfoAsync()).ToLite();
        return (T)(IEntity)lite.RetrieveAndRemember();
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

}
