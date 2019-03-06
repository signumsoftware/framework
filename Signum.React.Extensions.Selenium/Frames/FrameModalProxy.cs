using System;
using System.Diagnostics;
using OpenQA.Selenium;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public class FrameModalProxy<T> : ModalProxy, ILineContainer<T>, IEntityButtonContainer<T>, IValidationSummaryContainer where T : ModifiableEntity
    {
        public PropertyRoute Route { get; private set; }

        public FrameModalProxy(IWebElement element, PropertyRoute route = null)
            : base(element)
        {
            this.Route = route?? PropertyRoute.Root(typeof(T)) ;
        }

        public IWebElement ContainerElement()
        {
            return this.Element;
        }

        //public void CloseDiscardChanges()   not in use
        //{
        //    this.CloseButton.Find().Click();

        //    Selenium.ConsumeAlert();

        //    this.WaitNotVisible();
        //}

        public override void Dispose()
        {
            if (!AvoidClose)
            {
                string confirmationMessage = null;
                Selenium.Wait(() =>
                {
                    if (this.Element.IsStale())
                        return true;

                    if (TryToClose())
                        return true;

                    if (MessageModalProxyExtensions.IsMessageModalPresent(this.Selenium))
                    {
                        var alert = MessageModalProxyExtensions.GetMessageModal(this.Selenium);
                        alert.Click(MessageModalButton.Yes);
                    }

                    return false;

                }, () => "popup {0} to disapear with or without confirmation".FormatWith());

                if (confirmationMessage != null)
                    throw new InvalidOperationException(confirmationMessage);
            }

            Disposing?.Invoke(this.OkPressed);
        }

        [DebuggerStepThrough]
        private bool TryToClose()
        {
            try
            {
                var close = this.CloseButton.TryFind();
                if (close?.Displayed == true)
                {
                    close.Click();
                }

                return false;
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }
        }

        public EntityInfoProxy EntityInfo()
        {
            return EntityInfoProxy.Parse(this.Element.FindElement(By.CssSelector("div.sf-main-control")).GetAttribute("data-main-entity"));
        }

        public FrameModalProxy<T> WaitLoaded()
        {
            this.Element.WaitElementPresent(By.CssSelector("div.sf-main-control"));
            return this;
        }
    }
}
