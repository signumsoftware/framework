using OpenQA.Selenium;

namespace Signum.Selenium;

public class PaginationSelectorProxy
{
    public IWebElement Element { get; private set; }
    SearchControlProxy searchControl;

    public PaginationSelectorProxy(SearchControlProxy searchControl)
    {
        this.searchControl = searchControl;
        this.Element = searchControl.Element.FindElement(By.ClassName("sf-search-footer"));
    }

    public WebElementLocator ElementsPerPageElement
    {
        get { return Element.WithLocator(By.CssSelector("select.sf-elements-per-page")); }
    }

    public void SetElementsPerPage(int elementPerPage)
    {
        searchControl.WaitSearchCompleted(() =>
        {
            ElementsPerPageElement.Find().SelectElement().SelectByValue(elementPerPage.ToString());
        });
    }

    public WebElementLocator PaginationModeElement
    {
        get { return this.Element.WithLocator(By.CssSelector("select.sf-pagination-mode")); }
    }

    public void SetPaginationMode(PaginationMode mode)
    {
        PaginationModeElement.Find().SelectElement().SelectByValue(mode.ToString());
    }
}
