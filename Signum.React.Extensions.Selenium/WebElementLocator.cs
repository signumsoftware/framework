using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace Signum.React.Selenium
{
    public class WebElementLocator
    {
        public IWebElement ParentElement;
        public By Locator;

        public WebElementLocator(IWebElement parentElement, By locator)
        {
            this.ParentElement = parentElement;
            this.Locator = locator;
        }

        public IWebElement Find()
        {
           return ParentElement.FindElement(this.Locator);
        }

        public ReadOnlyCollection<IWebElement> FindElements()
        {
            return ParentElement.FindElements(this.Locator);
        }

        public IWebElement TryFind()
        {
            return ParentElement.TryFindElement(this.Locator);
        }

        public bool IsPresent()
        {
            return ParentElement.IsElementPresent(this.Locator);
        }

        public bool IsVisible()
        {
            return ParentElement.IsElementVisible(this.Locator);
        }

        public IWebElement WaitPresent()
        {
            return ParentElement.WaitElementPresent(this.Locator);
        }
        public void WaitNoPresent()
        {
            ParentElement.WaitElementNotPresent(this.Locator);
        }

        public IWebElement WaitVisible()
        {
            return ParentElement.WaitElementVisible(this.Locator);
        }

        public void WaitNoVisible()
        {
            ParentElement.WaitElementNotVisible(this.Locator);
        }

        public void AssertNotPresent()
        {
            ParentElement.AssertElementNotPresent(this.Locator);
        }

        public void AssertNotVisible()
        {
            ParentElement.AssertElementNotVisible(this.Locator);
        }

        public void AssertPresent()
        {
            ParentElement.AssertElementPresent(this.Locator);
        }

        public WebElementLocator CombineCss(string cssSelectorSuffix)
        {
            return new WebElementLocator(this.ParentElement, this.Locator.CombineCss(cssSelectorSuffix));
        }

    }

}
