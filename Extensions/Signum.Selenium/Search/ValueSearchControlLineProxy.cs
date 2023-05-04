using OpenQA.Selenium;

namespace Signum.Selenium;

public class SearchValueLineProxy
{
    public WebDriver Selenium { get; private set; }

    public IWebElement Element { get; private set; }

    public SearchValueLineProxy(IWebElement element)
    {
        this.Selenium = element.GetDriver();
        this.Element = element;
    }

    public WebElementLocator CountSearch
    {
        get { return this.Element.WithLocator(By.CssSelector(".count-search")); }
    }

    public WebElementLocator FindButton
    {
        get { return this.Element.WithLocator(By.CssSelector(".sf-line-button.sf-find")); }
    }

    public WebElementLocator CreateButton
    {
        get { return this.Element.WithLocator(By.CssSelector(".sf-line-button.sf-create")); }
    }

    public FrameModalProxy<T> Create<T>() where T : ModifiableEntity
    {
        var popup = this.CreateButton.Find().CaptureOnClick();

        if (SelectorModalProxy.IsSelector(popup))
            popup = popup.AsSelectorModal().SelectAndCapture<T>();

        return new FrameModalProxy<T>(popup).WaitLoaded();
    }
}
