using Microsoft.Playwright;

namespace Signum.Playwright.ModalProxies;

public class ErrorModalProxy : ModalProxy
{
    public ErrorModalProxy(IPage page) : base(page)
    {
    }

    public ErrorModalProxy(IPage page, ILocator modalElement) : base(modalElement, page)
    {
    }

    public ILocator GetButton()
    {
        return Modal.Locator(".sf-ok-button");
    }

    public async Task ClickOkAsync()
    {
        await GetButton().ClickAsync();
    }

    public async Task ClickOkWaitCloseAsync()
    {
        await GetButton().ClickAsync();
        await WaitForCloseAsync();
    }

    public async Task<string> GetBodyTextAsync()
    {
        var text = await Modal.Locator(".modal-body").TextContentAsync();
        return text?.Trim() ?? "";
    }

    public async Task<string> GetTitleTextAsync()
    {
        var text = await Modal.Locator(".modal-title").TextContentAsync();
        return text?.Trim() ?? "";
    }

    public async Task ThrowErrorModalAsync()
    {
        var header = Modal.Locator(".modal-header");

        var hasErrorClass = await header.EvaluateAsync<bool>(
            "el => el.classList.contains('dialog-header-error')");

        if (!hasErrorClass)
            throw new InvalidOperationException("The modal is not an error!");

        var title = await GetTitleTextAsync();
        var body = await GetBodyTextAsync();

        throw new ErrorModalException(title, body);
    }
}

[Serializable]
public class ErrorModalException : Exception
{
    public string Title { get; }
    public string Body { get; }
    
    public ErrorModalException(string title, string body) : base(title + "\n\n" + body)
    {
        this.Title = title;
        this.Body = body;
    }
}

public static class ErrorModalExtensions
{
    public static async Task<ErrorModalProxy?> GetErrorModalAsync(this IPage page)
    {
        var count = await page.Locator(".error-modal").CountAsync();

        if (count == 0)
            return null;

        var element = page.Locator(".error-modal").First;

        var parent = page.Locator(".modal-dialog").Filter(new LocatorFilterOptions
        {
            Has = element
        });

        return new ErrorModalProxy(page, parent);
    }
}
