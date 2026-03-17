using Microsoft.Playwright;

namespace Signum.Playwright.ModalProxies;

public class ErrorModalProxy : ModalProxy
{
    public IPage Page { get; }
    public ILocator Element { get; }

    public ErrorModalProxy(ILocator element, IPage page)
        : base(element, page)
    {
        this.Element = element;
        this.Page = page;
    }

    public ILocator GetButton() => Element.Locator(".sf-ok-button");

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
        var body = await Element.Locator(".modal-body").InnerTextAsync();
        return body.Trim();
    }

    public async Task<string> TitleTextAsync()
    {
        var title = await Element.Locator(".modal-title").InnerTextAsync();
        return title.Trim();
    }

    public async Task ThrowErrorModalAsync()
    {
        var header = Element.Locator(".modal-header");
        if (!await header.IsVisibleAsync() || !await header.EvaluateAsync<bool>("el => el.classList.contains('dialog-header-error')"))
            throw new InvalidOperationException("The modal is not an error!");

        throw new ErrorModalException(
            await TitleTextAsync(),
            await BodyTextAsync()
        );
    }

    public async Task WaitNotVisibleAsync()
    {
        await Element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
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

        return new ErrorModalProxy(element.Locator(".."), page);
    }
}
