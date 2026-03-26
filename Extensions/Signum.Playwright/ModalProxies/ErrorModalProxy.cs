using Microsoft.Playwright;

namespace Signum.Playwright.ModalProxies;

/// <summary>
/// Proxy for ErrorModal.tsx
/// </summary>
public class ErrorModalProxy : ModalProxy
{

    public ErrorModalProxy(ILocator element)
        : base(element)
    {
    }

    public ILocator GetButton() => Modal.Locator(".sf-ok-button");

    public async Task ClickOkAsync()
    {
        await GetButton().ClickAsync();
    }

    public async Task ClickOkAndWaitCloseAsync()
    {
        await GetButton().ClickAsync();
        await WaitNotVisibleAsync();
    }

    public async Task<string> BodyTextAsync()
    {
        var body = await Modal.Locator(".modal-body").InnerTextAsync();
        return body.Trim();
    }

    public async Task<string> TitleTextAsync()
    {
        var title = await Modal.Locator(".modal-title").InnerTextAsync();
        return title.Trim();
    }

    public async Task ThrowErrorModalAsync()
    {
        var header = Modal.Locator(".modal-header");
        if (!await header.IsVisibleAsync() || !await header.EvaluateAsync<bool>("el => el.classList.contains('dialog-header-error')"))
            throw new InvalidOperationException("The modal is not an error!");

        throw new ErrorModalException(
            await TitleTextAsync(),
            await BodyTextAsync()
        );
    }

    public async Task WaitNotVisibleAsync()
    {
        await Modal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }
}

[Serializable]
public class ErrorModalException : Exception
{
    public string Title { get; }
    public string Body { get; }

    public ErrorModalException(string title, string body)
        : base(title + "\n\n" + body)
    {
        this.Title = title;
        this.Body = body;
    }
}

public static class ErrorModalExtensions
{
    public static async Task<ErrorModalProxy?> GetErrorModalAsync(this IPage page)
    {
        var element = page.Locator(".error-modal");

        if (await element.CountAsync() == 0)
            return null;

        return new ErrorModalProxy(element.GetParent());
    }
}
