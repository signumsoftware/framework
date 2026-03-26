using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.LineProxies;
using Signum.Playwright.ModalProxies;
using Signum.Workflow;

namespace Signum.Playwright.Workflow;

/// <summary>
/// Proxy of CaseFrameModal.tsx
/// </summary>
public class CaseFrameModalProxy<T> : ModalProxy, ILineContainer<T>, IEntityButtonContainer<T>, IValidationSummaryContainer 
    where T : ICaseMainEntity
{
    public PropertyRoute Route { get; }
    public CaseFrameModalProxy(ILocator locator, PropertyRoute? route = null) : base(locator)
    {
        Route = route ?? PropertyRoute.Root(typeof(T));
    }

    public ILocator Container => Modal;
    ILocator ILineContainer.Element => Modal;
    ILocator IValidationSummaryContainer.Element => Modal;

    public override async ValueTask DisposeAsync()
    {
        if (!AvoidClose)
        {
            await this.Modal.Page.WaitAsync(async () =>
            {
                if (!await Modal.IsVisibleAsync())
                    return true;

                if (await TryToCloseAsync())
                    return true;

                var message = await Modal.Page.GetMessageModalAsync();
                if (message != null)
                {
                    await message.ClickAsync(MessageModalButton.Yes);
                }

                return false;
            });
        }

        Disposing?.Invoke(this.OkPressed);
    }
    private async Task<bool> TryToCloseAsync()
    {
        if (await this.CloseButton.IsVisibleAsync())
        {
            await this.CloseButton.ClickAsync();
            return false;
        }
        else
        {
            return true;
        }
    }

    public async Task<EntityInfoProxy> GetEntityInfoAsync()
    {
        var mainControl = Modal.Locator("div.sf-main-control");
        var attr = await mainControl.GetAttributeAsync("data-main-entity");
        
        if (attr == null)
            throw new InvalidOperationException("data-main-entity attribute not found");

        return EntityInfoProxy.Parse(attr)!;
    }
    public async Task<CaseFrameModalProxy<T>> WaitLoadedAsync()
    {
        await Modal.Locator("div.sf-main-control").WaitVisibleAsync();
        return this;
    }
}

public static class FrameModalProxyExtension
{
    public static async Task<FrameModalProxy<T>> Await_AsFrameModal<T>(this Task<ILocator> modal)
    where T : ModifiableEntity
    {
        return new FrameModalProxy<T>(await modal);
    }

    public static FrameModalProxy<T> AsFrameModal<T>(this ILocator modal)
        where T : ModifiableEntity
    {
        return new FrameModalProxy<T>(modal);
    }
}
