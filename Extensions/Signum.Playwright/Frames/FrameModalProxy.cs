using Microsoft.Playwright;
using Signum.Playwright.LineProxies;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Frames;

//Proxy for FrameModal.tsx
public class FrameModalProxy<T> : ModalProxy, ILineContainer<T>, IEntityButtonContainer<T>, IValidationSummaryContainer 
    where T : ModifiableEntity
{
    public PropertyRoute Route { get; }
    public FrameModalProxy(ILocator locator, PropertyRoute? route = null) : base(locator)
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
            var maxAttempts = 10;
            var attempts = 0;

            while (attempts < maxAttempts)
            {
                attempts++;

                if (!await Modal.IsVisibleAsync())
                    break;

                if (await TryToCloseAsync())
                    break;

                var message = await Modal.Page.GetMessageModalAsync();
                if (message != null)
                {
                    await message.ClickAsync(MessageModalButton.Yes);
                    await Task.Delay(300);
                }

                await Task.Delay(200);
            }
        }

        Disposing?.Invoke(this.OkPressed);
    }
    private async Task<bool> TryToCloseAsync()
    {
        try
        {
            var closeCount = await this.CloseButton.CountAsync();
            if (closeCount == 0)
                return false;

            var close = this.CloseButton.First;
            if (await close.IsVisibleAsync())
            {
                await close.ClickAsync();
            }
            return false;
        }
        catch (Exception)
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
    public async Task<FrameModalProxy<T>> WaitLoadedAsync()
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
