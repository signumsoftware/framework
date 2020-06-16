using OpenQA.Selenium;
using Signum.Utilities;

namespace Signum.React.Selenium
{

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
                this.Element.FindElement(By.ClassName("rw-dropdown-list")).Click();
            }

            var container = this.Element.WaitElementVisible(By.ClassName("rw-popup-container"));

            var selector = key.HasText() ?
                By.CssSelector("li > span[data-token='" + key + "']") :
                By.CssSelector("li > span:not([data-token])");

            var elem = container.WaitElementVisible(selector);
            elem.ScrollTo();
            elem.Click();
        }
    }
}
