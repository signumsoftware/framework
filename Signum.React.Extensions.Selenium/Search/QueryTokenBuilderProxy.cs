using System.Linq;
using OpenQA.Selenium;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public class QueryTokenBuilderProxy
    {
        public IWebElement Element { get; private set; }

        public QueryTokenBuilderProxy(IWebElement element)
        {
            this.Element = element;
        }

        public WebElementLocator TokenElement(int tokenIndex)
        {
            return this.Element.WithLocator(By.CssSelector($".sf-query-token-part:nth-child({tokenIndex + 1}"));
        }

        public void SelectToken(string token)
        {
            string[] parts = token.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                var prev = parts.Take(i).ToString(".");

                var qt = new QueryTokenPartProxy(TokenElement(i).WaitPresent());

                qt.Select(parts[i]);
            }

            //Selenium.Wait(() =>
            //{
            //    var tokenLocator = TokenElement(parts.Length, token, isEnd: false);
            //    if (Selenium.IsElementPresent(tokenLocator))
            //    {
            //        new
            //        Selenium.FindElement(tokenLocator).SelectElement().SelectByValue("");
            //        return true;
            //    }

            //    if (Selenium.IsElementPresent(TokenElement(parts.Length, token, isEnd: true)))
            //        return true;

            //    return false;
            //});
        }
    }
}
