using OpenQA.Selenium;

namespace Signum.Selenium;

public class ModalProxy : IDisposable
{
    public WebDriver Selenium { get; private set; }

    public IWebElement Element { get; private set; }

    public ModalProxy(IWebElement element)
    {
        this.Selenium = element.GetDriver();
        this.Element = element;

        if (!this.Element.HasClass("modal"))
            throw new InvalidOperationException("Not a valid modal");
    }

    public WebElementLocator CloseButton
    {
        get { return this.Element.WithLocator(By.CssSelector(".modal-header button.btn-close")); }
    }

    public bool AvoidClose { get; set; }

    public virtual void Dispose()
    {
        if (this.Selenium.GetMessageModal() == null)
            if (!AvoidClose)
            {
                try
                {
                    if (this.Element.IsStale())
                        return;

                    var button = this.CloseButton.TryFind();
                    if (button != null && button.Displayed)
                        button.SafeClick();
                }
                //catch (ElementNotVisibleException)
                //{
                //}
                catch (StaleElementReferenceException)
                {
                }

                this.WaitNotVisible();
            }

        Disposing?.Invoke(OkPressed);
    }

    public Action<bool>? Disposing;

    public WebElementLocator OkButton
    {
        get { return this.Element.WithLocator(By.CssSelector(".sf-entity-button.sf-ok-button")); }
    }


    public FrameModalProxy<T> OkWaitFrameModal<T>() where T : ModifiableEntity
    {
        var element = this.OkButton.Find().CaptureOnClick();
        var disposing = this.Disposing;
        this.Disposing = null;
        return new FrameModalProxy<T>(element) { Disposing = disposing };
    }

    public SearchModalProxy OkWaitSearchModal()
    {
        var element = this.OkButton.Find().CaptureOnClick();
        var disposing = this.Disposing;
        this.Disposing = null;
        return new SearchModalProxy(element) { Disposing = disposing };
    }

    public bool OkPressed;
    public void OkWaitClosed(bool consumeAlert = false)
    {
        this.OkButton.Find().SafeClick();

        if (consumeAlert)
            MessageModalProxyExtensions.CloseMessageModal(this.Selenium, MessageModalButton.Ok);

        this.WaitNotVisible();
        this.OkPressed = true;
    }

    public void WaitNotVisible()
    {
        this.Element.GetDriver().Wait(() =>
        {
            try
            {
                return this.Element == null || !this.Element.Displayed;
            }
            catch (NoSuchElementException)
            {
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }
        });
    }

    public void Close()
    {
        this.CloseButton.Find().SafeClick();
        this.WaitNotVisible();
    }
}
