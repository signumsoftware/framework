using OpenQA.Selenium;

namespace Signum.Selenium;


public class QueryTokenPartProxy
{
    public IWebElement Element { get; private set; }

    public QueryTokenPartProxy(IWebElement element)
    {
        this.Element = element;
    }

    public void Select(string? fullKey)
    {
        if (!this.Element.IsElementVisible(By.ClassName("rw-popup-container")))
        {
            this.Element.FindElement(By.CssSelector(".rw-dropdown-list,.sf-query-token-plus")).SafeClick();
        }

        var dropdownContainer = this.Element.WaitElementVisible(By.ClassName("rw-popup-container"));
        
        var tokenSelector = fullKey.HasText() ? $"[data-full-token='{fullKey}']" : ":not([0])";

        var optionElement = dropdownContainer.WaitElementVisible(By.CssSelector($"div > span{tokenSelector}"));
        optionElement.SafeClick();

        Element.WaitElementVisible(By.CssSelector($".rw-dropdown-list-value span{tokenSelector}"));
    }
}
