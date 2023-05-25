using OpenQA.Selenium;

namespace Signum.Selenium;


public class QueryTokenPartProxy
{
    public IWebElement Element { get; private set; }

    public QueryTokenPartProxy(IWebElement element)
    {
        this.Element = element;
    }

    public void Select(string? key)
    {
        if (!this.Element.IsElementVisible(By.ClassName("rw-popup-container")))
        {
            this.Element.FindElement(By.ClassName("rw-dropdown-list")).SafeClick();
        }

        var container = this.Element.WaitElementVisible(By.ClassName("rw-popup-container"));

        var selector = key.HasText() ?
            By.CssSelector("div > span[data-token='" + key + "']") :
            By.CssSelector("div > span:not([data-token])");

        var elem = container.WaitElementVisible(selector);
        elem.ScrollTo();
        elem.Click();
    }
}
