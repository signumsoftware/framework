using System.Text;
using OpenQA.Selenium;
using Signum.Engine.Basics;
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

        public void Select(string key)
        {
            this.Element.FindElement(By.ClassName("rw-dropdownlist")).Click();

            var container = this.Element.WaitElementVisible(By.ClassName("rw-popup-container"));

            if (key.HasText())
                container.FindElement(By.CssSelector("li > span[data-token=" + key + "]")).Click();
            else
                container.FindElement(By.CssSelector("li > span:not([data-token])")).Click();
        }
    }
}
