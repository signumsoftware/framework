using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Entities;

namespace Signum.React.Selenium
{
    public class ModalProxy : IDisposable
    {
        public RemoteWebDriver Selenium { get; private set; }

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
            get { return this.Element.WithLocator(By.CssSelector(".modal-header button.close")); }
        }

        public bool AvoidClose { get; set; }

        public virtual void Dispose()
        {
            if (!MessageModalProxyExtensions.IsMessageModalPresent(this.Selenium))
                if (!AvoidClose)
                {
                    try
                    {
                        if (this.Element.IsStale())
                            return;

                        var button = this.CloseButton.TryFind();
                        if (button != null && button.Displayed)
                            button.Click();
                    }
                    catch (ElementNotVisibleException)
                    {
                    }
                    catch (StaleElementReferenceException)
                    {
                    }

                    this.WaitNotVisible();
                }

            Disposing?.Invoke(OkPressed);
        }

        public Action<bool> Disposing;

        public WebElementLocator OkButton
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-entity-button.sf-ok-button")); }
        }


        public FrameModalProxy<T> OkWaitPopupControl<T>() where T : Entity
        {
            var element = this.OkButton.Find().CaptureOnClick();
            var disposing = this.Disposing;
            this.Disposing = null;
            return new FrameModalProxy<T>(element) { Disposing = disposing };
        }

        public bool OkPressed;
        public void OkWaitClosed(bool consumeAlert = false)
        {
            this.OkButton.Find().Click();

            if (consumeAlert)
                MessageModalProxyExtensions.CloseMessageModal(this.Selenium, MessageModalButton.Ok);

            this.WaitNotVisible();
            this.OkPressed = true;
        }

        public void WaitNotVisible()
        {
            this.Element.GetDriver().Wait(() => this.Element.IsStale());
        }

        public void Close()
        {
            this.CloseButton.Find().Click();
            this.WaitNotVisible();
        }
    }
}
