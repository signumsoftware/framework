using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace Signum.Selenium;

public class WebElementLocator
{
    public ISearchContext Parent;
    public By Locator;

    public WebElementLocator(ISearchContext parent, By locator)
    {
        this.Parent = parent;
        this.Locator = locator;
    }

    public IWebElement Find()
    {
       return Parent.FindElement(this.Locator);
    }

    public ReadOnlyCollection<IWebElement> FindElements()
    {
        return Parent.FindElements(this.Locator);
    }

    public IWebElement? TryFind()
    {
        return Parent.TryFindElement(this.Locator);
    }

    public bool IsPresent()
    {
        return Parent.IsElementPresent(this.Locator);
    }

    public bool IsVisible()
    {
        return Parent.IsElementVisible(this.Locator);
    }

    public IWebElement WaitPresent()
    {
        return Parent.WaitElementPresent(this.Locator);
    }
    public void WaitNoPresent()
    {
        Parent.WaitElementNotPresent(this.Locator);
    }

    public IWebElement WaitVisible(bool scrollTo = false)
    {
        var element = Parent.WaitElementVisible(this.Locator);
        return scrollTo ? element.ScrollTo() : element;
    }

    public void WaitNoVisible()
    {
        Parent.WaitElementNotVisible(this.Locator);
    }

    public void AssertNotPresent()
    {
        Parent.AssertElementNotPresent(this.Locator);
    }

    public void AssertNotVisible()
    {
        Parent.AssertElementNotVisible(this.Locator);
    }

    public void AssertPresent()
    {
        Parent.AssertElementPresent(this.Locator);
    }

    public WebElementLocator CombineCss(string cssSelectorSuffix)
    {
        return new WebElementLocator(this.Parent, this.Locator.CombineCss(cssSelectorSuffix));
    }

}

