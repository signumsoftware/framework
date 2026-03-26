using Microsoft.Playwright;
using Signum.Playwright.LineProxies;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Frames;

//Proxy for FrameModal.tsx
public class FrameModalProxy<T> : ModalProxy, ILineContainer<T>, IEntityButtonContainer<T>, IValidationSummaryContainer 
    where T : ModifiableEntity
{
    public PropertyRoute Route { get; }
    FrameModalProxy(ILocator locator, PropertyRoute? route = null) : base(locator)
    {
        Route = route ?? PropertyRoute.Root(typeof(T));
    }

    public static async Task<FrameModalProxy<T>> NewAsync(ILocator modal, PropertyRoute? route = null)
    {
        var result = new FrameModalProxy<T>(modal, route);
        await result.MainControl.WaitVisibleAsync();    
        return result;
    }

    ILocator IEntityButtonContainer.Container => Modal;
    ILocator ILineContainer.Element => MainControl;
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

    public ILocator MainControl => Modal.Locator("div.sf-main-control");


    public Task<EntityInfoProxy> GetEntityInfoAsync() => EntityInfoProxy.GetFromMainEntityAsync(MainControl);
}

public static class FrameModalProxyExtension
{
    public static async Task<FrameModalProxy<T>> Await_AsFrameModal<T>(this Task<ILocator> modal)
    where T : ModifiableEntity
    {
        return await FrameModalProxy<T>.NewAsync(await modal);
    }

    public static Task<FrameModalProxy<T>> AsFrameModal<T>(this ILocator modal)
        where T : ModifiableEntity
    {
        return FrameModalProxy<T>.NewAsync(modal);
    }
}
