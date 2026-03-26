using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.Search;
using System.Runtime.CompilerServices;

namespace Signum.Playwright.ModalProxies;

/// <summary>
/// Generic proxy for bootstrap Modals
/// </summary>
public class ModalProxy : IDisposable, IAsyncDisposable
{
    public ILocator Modal { get; }

    public Func<Task>? AfterClose { get; set; }

    public ModalProxy(ILocator locator)
    {
        Modal = locator;
    }

    public ILocator CloseButton =>
        Modal.Locator(".modal-header button.btn-close");
    
    public bool AvoidClose { get; set; }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (!AvoidClose)
        {
            try
            {
                if (await Modal.IsVisibleAsync())
                {
                    var close = CloseButton;

                    if (await close.IsVisibleAsync())
                        await close.ClickAsync();
                }
            }
            catch
            {
            }

            await WaitForCloseAsync();
        }

        if (Disposing != null)
            await Disposing(OkPressed);

        if (AfterClose != null)
            await AfterClose();
    }

    public Func<bool, Task>? Disposing;

    public ILocator OkButton =>
        Modal.Locator(".sf-entity-button.sf-ok-button");

    public async Task<FrameModalProxy<T>> OkWaitFrameModalAsync<T>()
    where T : ModifiableEntity
    {
        var newModal = await Modal.Page.CaptureModalAsync(async () =>
        {
            await OkButton.ClickAsync();
        });

        var disposing = Disposing;
        Disposing = null;

        var result = await FrameModalProxy<T>.NewAsync(newModal.Modal);
        result.Disposing = disposing;
        return result;
    }

    public async Task<SearchModalProxy> OkWaitSearchModalAsync()
    {
        var newModal = await Modal.Page.CaptureModalAsync(async () =>
        {
            await OkButton.ClickAsync();
        });

        var disposing = Disposing;
        Disposing = null;

        return new SearchModalProxy(newModal)
        {
            Disposing = disposing
        };
    }
    
    public bool OkPressed;

    public async Task OkWaitClosedAsync(bool consumeAlert = false)
    {
        await OkButton.ClickAsync();

        if (consumeAlert)
        {
            var alert = Modal.Locator(".modal-dialog .message-modal");
            if (await alert.IsVisibleAsync())
            {
                await alert.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();
            }
        }

        await WaitForCloseAsync();

        OkPressed = true;
    }

    public async Task WaitForCloseAsync()
    {
        await Modal.WaitForAsync(new()
        {
            State = WaitForSelectorState.Hidden
        });
    }

    public async Task CloseAsync()
    {
        await CloseButton.ClickAsync();
        await WaitForCloseAsync();
    }

}

public static class ModalProxyExtensions
{

    public static async Task<ModalProxy> Await_AsModalProxy(this Task<ILocator> locator)
    {
        return new ModalProxy(await locator);
    }
}
