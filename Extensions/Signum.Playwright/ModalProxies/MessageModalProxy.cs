using Microsoft.Playwright;

namespace Signum.Playwright.ModalProxies;

public class MessageModalProxy : ModalProxy
{
    public MessageModalProxy(IPage page) : base(page)
    {
    }

    public MessageModalProxy(IPage page, ILocator modalElement) : base(modalElement, page)
    {
    }

    public ILocator GetButton(MessageModalButton button)
    {
        var className =
            button == MessageModalButton.Yes ? "sf-yes-button" :
            button == MessageModalButton.No ? "sf-no-button" :
            button == MessageModalButton.Ok ? "sf-ok-button" :
            button == MessageModalButton.Cancel ? "sf-cancel-button" :
            throw new NotImplementedException("Unexpected button");

        return Modal.Locator($".{className}");
    }

    public async Task ClickAsync(MessageModalButton button)
    {
        await GetButton(button).ClickAsync();
    }

    public async Task ClickWaitCloseAsync(MessageModalButton button)
    {
        await GetButton(button).ClickAsync();
        await WaitForCloseAsync();
    }

    public async Task<string> GetBodyTextAsync() => 
        await Modal.Locator(".modal-body").TextContentAsync() ?? "";

    public async Task<string> GetTitleTextAsync() => 
        await Modal.Locator(".modal-title").TextContentAsync() ?? "";
}

public static class MessageModalProxyExtensions
{
    public static async Task<MessageModalProxy?> GetMessageModalAsync(this IPage page)
    {
        var count = await page.Locator(".message-modal").CountAsync();

        if (count == 0)
            return null;

        var element = page.Locator(".message-modal").First;

        var parent = page.Locator(".modal-dialog").Filter(new LocatorFilterOptions
        {
            Has = element
        });

        return new MessageModalProxy(page, parent);
    }

    public static async Task CloseMessageModalAsync(this IPage page, MessageModalButton button)
    {
        var message = await GetMessageModalAsync(page);
        if (message == null)
            throw new InvalidOperationException("No message modal found");

        await message.ClickAsync(button);
    }
}

public enum MessageModalButton
{
    Yes,
    No,
    Ok,
    Cancel
}
