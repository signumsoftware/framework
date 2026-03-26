using Signum.Playwright.Frames;
using Signum.Playwright.LineProxies;
using Signum.Playwright.ModalProxies;
using Signum.Workflow;

namespace Signum.Playwright.Workflow;

/// <summary>
/// Proxy of CaseFramePage.tsx
/// </summary>
public class CaseFramePageProxy<T> : ILineContainer<T>, IEntityButtonContainer<T>, IWidgetContainer, IValidationSummaryContainer, IAsyncDisposable, IDisposable
    where T : ICaseMainEntity
{
    public IPage Page { get; }
    public ILocator Element { get; }
    public PropertyRoute Route { get; }

    private CaseFramePageProxy(IPage page)
    {
        Page = page;
        Element = page.Locator(".normal-control");
        Route = PropertyRoute.Root(typeof(T));
    }

    public static async Task<CaseFramePageProxy<T>> NewAsync(IPage page)
    {
        var result = new CaseFramePageProxy<T>(page);
        await result.WaitLoadedAsync();
        return result;
    }

    public ILocator Container => Element;

    public Func<Task>? OnDisposed { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (OnDisposed != null)
            await OnDisposed.Invoke();
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

    async Task WaitLoadedAsync()
    {
        await Page.WaitForSelectorAsync(".normal-control");

        var error = await Page.GetErrorModalAsync();
        if (error != null)
        {
            await error.ThrowErrorModalAsync();
        }

        await MainControl.WaitVisibleAsync();
    }

}
