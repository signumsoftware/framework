using Microsoft.Playwright;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Frames;

public class FrameModalProxy<T> : ModalProxy, ILineContainer<T>, IEntityButtonContainer<T>, IValidationSummaryContainer 
    where T : ModifiableEntity
{
    public PropertyRoute Route { get; }

    ILocator ILineContainer.Element => Modal;
    ILocator IEntityButtonContainer<T>.Element => Modal;
    ILocator IValidationSummaryContainer.Element => Modal;

    public FrameModalProxy(IPage page, PropertyRoute? route = null) : base(page)
    {
        Route = route ?? PropertyRoute.Root(typeof(T));
    }

    public FrameModalProxy(IPage page, ILocator modalElement, PropertyRoute? route = null) : base(modalElement, page)
    {
        Route = route ?? PropertyRoute.Root(typeof(T));
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

    public bool AvoidClose { get; set; }
    public Action<bool>? Disposing { get; set; }
    public bool OkPressed { get; private set; }

    public ILocator CloseButtonLocator => Modal.Locator("button.close, .btn-close");

    public override async Task OkAsync()
    {
        OkPressed = true;
        await base.OkAsync();
    }

    private async Task<bool> TryToCloseAsync()
    {
        try
        {
            var closeCount = await CloseButtonLocator.CountAsync();
            if (closeCount == 0)
                return false;

            var close = CloseButtonLocator.First;
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

    public async Task DisposeAsync()
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

                var message = await Page.GetMessageModalAsync();
                if (message != null)
                {
                    await message.ClickAsync(MessageModalButton.Yes);
                    await Task.Delay(300);
                }

                await Task.Delay(200);
            }
        }

        Disposing?.Invoke(OkPressed);
    }
}

public static class FrameModalProxyExtension
{
    public static FrameModalProxy<T> AsFrameModal<T>(this ModalProxy modal)
        where T : ModifiableEntity
    {
        return new FrameModalProxy<T>(modal.Page, modal.Modal);
    }
}
