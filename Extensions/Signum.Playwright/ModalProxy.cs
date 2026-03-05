using Microsoft.Playwright;

namespace Signum.Playwright;

/// <summary>
/// Proxy for working with Signum Framework modals
/// Handles Bootstrap modal dialogs
/// </summary>
public class ModalProxy
{
    public IPage Page { get; }
    public ILocator Modal { get; }
    public Func<Task>? AfterClose { get; set; }

    public ModalProxy(IPage page, int modalIndex = -1)
    {
        Page = page;

        if (modalIndex >= 0)
        {
            Modal = page.Locator(".modal-dialog").Nth(modalIndex);
        }
        else
        {
            // Get the topmost (last) modal
            Modal = page.Locator(".modal-dialog").Last;
        }
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

    public async Task WaitForModalAsync()
    {
        await Modal.WaitVisibleAsync();
    }

    public async Task<string?> GetTitleAsync()
    {
        return await Modal.Locator(".modal-title").TextContentAsync();
    }

    public async Task SetFieldValueAsync(string fieldName, string value)
    {
        var field = Modal.Locator($"[data-member='{fieldName}'] .form-control")
            .Or(Modal.Locator($"input[name='{fieldName}']"))
            .First;

        await field.FillAsync(value);
    }

    public async Task<string?> GetFieldValueAsync(string fieldName)
    {
        var field = Modal.Locator($"[data-member='{fieldName}'] .form-control")
            .Or(Modal.Locator($"input[name='{fieldName}']"))
            .First;

        return await field.InputValueAsync();
    }

    public virtual async Task OkAsync()
    {
        await Modal.Locator("button.sf-entity-button-save, button.btn-primary:has-text('OK'), button.btn-primary:has-text('Save')").ClickAsync();
        await WaitForCloseAsync();
        
        if (AfterClose != null)
        {
            await AfterClose();
        }
    }

    public async Task CancelAsync()
    {
        await Modal.Locator("button.sf-close-button, button.btn-secondary:has-text('Cancel')").ClickAsync();
        await WaitForCloseAsync();
        
        if (AfterClose != null)
        {
            await AfterClose();
        }
    }

    public async Task CloseAsync()
    {
        await Modal.Locator("button.close, .btn-close").ClickAsync();
        await WaitForCloseAsync();
        
        if (AfterClose != null)
        {
            await AfterClose();
        }
    }

    public async Task WaitForCloseAsync()
    {
        await Modal.WaitNotVisibleAsync();
    }

    public async Task<bool> IsVisibleAsync()
    {
        return await Modal.IsVisibleAsync();
    }

    public async Task ClickButtonAsync(string buttonText)
    {
        await Modal.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = buttonText })
            .Or(Modal.Locator($"button:has-text('{buttonText}')"))
            .ClickAsync();
    }

    public ILocator Locator(string selector)
    {
        return Modal.Locator(selector);
    }

    public static async Task<ModalProxy> CaptureAsync(IPage page, Func<Task> clickAction)
    {
        var oldModals = await page.Locator(".modal-dialog").AllAsync();
        var oldModalSet = new HashSet<ILocator>(oldModals);

        await clickAction();

        // Wait for new modal
        await page.WaitForSelectorAsync(".modal-dialog");
        await Task.Delay(200); // Small delay for modal animation

        var newModals = await page.Locator(".modal-dialog").AllAsync();
        var newModal = newModals.FirstOrDefault(m => !oldModalSet.Contains(m));

        if (newModal == null)
            newModal = page.Locator(".modal-dialog").Last;

        return new ModalProxy(newModal, page);
    }
}
