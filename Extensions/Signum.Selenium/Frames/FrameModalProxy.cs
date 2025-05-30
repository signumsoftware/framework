using System.Diagnostics;
using OpenQA.Selenium;

namespace Signum.Selenium;

public class FrameModalProxy<T> : ModalProxy, ILineContainer<T>, IEntityButtonContainer<T>, IValidationSummaryContainer where T : ModifiableEntity
{
    public PropertyRoute Route { get; private set; }

    public FrameModalProxy(IWebElement element, PropertyRoute? route = null)
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
            string? confirmationMessage = null;
            Selenium.Wait(() =>
            {
                if (this.Element.IsStale())
                    return true;

                if (TryToClose())
                    return true;

                var message = this.Selenium.GetMessageModal()!;
                if (message != null)
                {
                    message.Click(MessageModalButton.Yes);
                }

                return false;

            }, () => "popup {0} to disapear with or without confirmation".FormatWith(this.Element));

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
        return EntityInfoProxy.Parse(this.Element.FindElement(By.CssSelector("div.sf-main-control")).GetDomAttributeOrThrow("data-main-entity"))!;
    }

    public FrameModalProxy<T> WaitLoaded()
    {
        this.Element.WaitElementPresent(By.CssSelector("div.sf-main-control"));
        return this;
    }
}

public static class FrameModalProxyExtension
{
    public static FrameModalProxy<T> AsFrameModal<T>(this IWebElement element) 
        where T: ModifiableEntity
    {
        return new FrameModalProxy<T>(element);
    }
}
