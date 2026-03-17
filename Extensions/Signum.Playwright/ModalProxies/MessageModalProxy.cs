using Microsoft.Playwright;

namespace Signum.Playwright.ModalProxies;

public class MessageModalProxy : ModalProxy
{
    public IPage Page { get; }
    public ILocator Element { get; }

    public MessageModalProxy(ILocator element, IPage page)
        : base(element, page)
    {
        this.Element = element;
        this.Page = page;
    }

    public ILocator GetButton(MessageModalButton button)
    {
        string className = button switch
        {
            MessageModalButton.Yes => "sf-yes-button",
            MessageModalButton.No => "sf-no-button",
            MessageModalButton.Ok => "sf-ok-button",
            MessageModalButton.Cancel => "sf-cancel-button",
            _ => throw new NotImplementedException("Unexpected button")
        };

        return Element.Locator($".{className}");
    }

    public async Task ClickAsync(MessageModalButton button)
    {
        await GetButton(button).ClickAsync();
    }

    public async Task ClickWaitCloseAsync(MessageModalButton button)
    {
        await GetButton(button).ClickAsync();
        await WaitNotVisibleAsync();
    }

    public async Task<string> BodyTextAsync()
    {
        var body = await Element.Locator(".modal-body").InnerTextAsync();
        return body.Trim();
    }

    public async Task<string> TitleTextAsync()
    {
        var title = await Element.Locator(".modal-title").InnerTextAsync();
        return title.Trim();
    }

    public async Task WaitNotVisibleAsync()
    {
        await Element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }
}

public static class MessageModalProxyExtensions
{
    public static async Task<MessageModalProxy?> GetMessageModalAsync(this IPage page)
    {
        var element = page.Locator(".message-modal");

        if (await element.CountAsync() == 0)
            return null;

        return new MessageModalProxy(element.Locator(".."), page);
    }

    public static async Task CloseMessageModalAsync(this IPage page, MessageModalButton button)
    {
        var message = await page.WaitForMessageModalAsync();
        if (message != null)
            await message.ClickWaitCloseAsync(button);
    }

    public static MessageModalProxy AsMessageModal(this ILocator element, IPage page)
    {
        return new MessageModalProxy(element, page);
    }

    public static async Task<MessageModalProxy?> WaitForMessageModalAsync(this IPage page, int timeoutMs = 5000)
    {
        var element = page.Locator(".message-modal");
        try
        {
            await element.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });
            return new MessageModalProxy(element.Locator(".."), page);
        }
        catch
        {
            return null;
        }
    }
}

public enum MessageModalButton
{
    Yes,
    No,
    Ok,
    Cancel
}
