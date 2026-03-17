using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.Search;

namespace Signum.Playwright.ModalProxies;

/// <summary>
/// Proxy for working with Signum Framework modals
/// </summary>
public class ModalProxy : IAsyncDisposable
{
    public IPage Page { get; }
    public ILocator Modal { get; }




    public Func<Task>? AfterClose { get; set; }

    public ModalProxy(IPage page, int modalIndex = -1)
    {
        Page = page;

        Modal = modalIndex >= 0
            ? page.Locator(".modal-dialog").Nth(modalIndex)
            : page.Locator(".modal-dialog").Last;
    }

    public ModalProxy(IPage page, string modalTitle)
    {
        Page = page;

        Modal = page.Locator(".modal-dialog")
            .Filter(new LocatorFilterOptions
            {
                Has = page.Locator($".modal-title:has-text('{modalTitle}')")
            });
    }

    public ModalProxy(ILocator modalElement, IPage page)
    {
        Page = page;
        Modal = modalElement;
    }

    public ILocator CloseButton =>
        Modal.Locator(".modal-header button.btn-close");

    public bool AvoidClose { get; set; }

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

        Disposing?.Invoke(OkPressed);

        if (AfterClose != null)
            await AfterClose();
    }

    public Action<bool>? Disposing;

    public ILocator OkButton =>
        Modal.Locator(".sf-entity-button.sf-ok-button");

    public async Task<FrameModalProxy<T>> OkWaitFrameModalAsync<T>()
    where T : ModifiableEntity
    {
        var newModal = await CaptureAsync(Page, async () =>
        {
            await OkButton.ClickAsync();
        });

        var disposing = Disposing;
        Disposing = null;

        return new FrameModalProxy<T>(Page, newModal.Modal)
        {
            Disposing = disposing
        };
    }

    public async Task<SearchModalProxy> OkWaitSearchModalAsync()
    {
        var newModal = await CaptureAsync(Page, async () =>
        {
            await OkButton.ClickAsync();
        });

        var disposing = Disposing;
        Disposing = null;

        return new SearchModalProxy(newModal.Modal, Page)
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
            var alert = Page.Locator(".modal-dialog .message-modal");
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

    public static async Task<ModalProxy> CaptureAsync(IPage page, Func<Task> clickAction)
    {
        var beforeCount = await page.Locator(".modal-dialog").CountAsync();

        await clickAction();

        await page.WaitForFunctionAsync(
            @"(before) => document.querySelectorAll('.modal-dialog').length > before",
            beforeCount
        );

        var modal = page.Locator(".modal-dialog").Nth(beforeCount);

        return new ModalProxy(modal, page);
    }

}
