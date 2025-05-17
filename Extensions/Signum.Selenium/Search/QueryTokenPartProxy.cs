using OpenQA.Selenium;

namespace Signum.Selenium;


public class QueryTokenPartProxy
{
    public IWebElement Element { get; private set; }

    public QueryTokenPartProxy(IWebElement element)
    {
        this.Element = element;
    }

    public void Select(string? key, By by)
    {
        try        
        {           
            if (!this.Element.IsElementVisible(By.ClassName("rw-popup-container")))
            {
                this.Element.FindElement(By.CssSelector(".rw-dropdown-list,.sf-query-token-plus")).SafeClick();
            }
        }
        catch(StaleElementReferenceException ex)
        {
            Logger.Log($"{ex.Message}\r\nElement:{Element}\r\n{by}");
            throw;
        }


        var container = this.Element.WaitElementVisible(By.ClassName("rw-popup-container"));

        var dataTokenAttr = key.HasText() ? $"[data-token='{key}']" : ":not([data-token])";
        var tokenElement = container.WaitElementVisible(By.CssSelector($"div > span{dataTokenAttr}"));
        tokenElement.SafeClick();

        Element.WaitElementVisible(By.CssSelector($".rw-dropdown-list-value span{dataTokenAttr}"));
    }
}
