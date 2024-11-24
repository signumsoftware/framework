using OpenQA.Selenium;

namespace Signum.Selenium;

public class ErrorModalProxy : ModalProxy
{
    public ErrorModalProxy(IWebElement element) : base(element)
    {
        if (!element.HasClass("modal") && this.Element.FindElement(By.ClassName("error-modal")) == null)
            throw new InvalidOperationException("Not a valid modal");
    }

    public IWebElement GetButton()
    {
        return this.Element.FindElement(By.ClassName("sf-ok-button"));
    }

    public void ClicOk()
    {
        this.GetButton().ButtonClick();
    }

    public void ClicOkkWaitClose()
    {
        this.GetButton().ButtonClick();
        this.WaitNotVisible();
    }

    public string BodyText => Element.FindElement(By.ClassName("modal-body")).Text.Trim();

    public string TitleText => Element.FindElement(By.ClassName("modal-title")).Text.Trim();

    public void ThrowErrorModal()
    {
        var header = this.Element.FindElement(By.ClassName("modal-header"));

        if (header == null || !header.HasClass("dialog-header-error"))
            throw new InvalidOperationException("The modal is not an error!");

        throw new ErrorModalException(this.TitleText, this.BodyText);
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

public static class ErrorModalExtension
{
    public static ErrorModalProxy? GetErrorModal(this WebDriver selenium)
    {
        var element = selenium.TryFindElement(By.ClassName("error-modal"));

        if (element == null)
            return null;

        return new ErrorModalProxy(element.GetParent());
    }
}
