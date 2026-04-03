using OpenQA.Selenium;

namespace Signum.Selenium;

public class SearchPageProxy : IDisposable
{
    public WebDriver Selenium { get; private set; }
    public SearchControlProxy SearchControl { get; private set; }
    public ResultTableProxy Results { get { return SearchControl.Results; } }
    public FiltersProxy Filters { get { return SearchControl.Filters; } }
    public PaginationSelectorProxy Pagination { get { return SearchControl.Pagination; } }

    public SearchPageProxy(WebDriver selenium)
    {
        this.Selenium = selenium;
        this.SearchControl = new SearchControlProxy(selenium.WaitElementVisible(By.CssSelector(".sf-search-page .sf-search-control")));
    }

    public FrameModalProxy<T> Create<T>() where T : ModifiableEntity
    {
        var popup = SearchControl.CreateButton.Find().CaptureOnClick();

        if (SelectorModalProxy.IsSelector(popup))
            popup = popup.AsSelectorModal().SelectAndCapture<T>();

        return new FrameModalProxy<T>(popup);
    }

    public FramePageProxy<T> CreateInPlace<T>() where T : ModifiableEntity
    {
        SearchControl.CreateButton.Find().Click();

        var result = new FramePageProxy<T>(this.Selenium);
 
        return result;
    }

    public FramePageProxy<T> CreateInTab<T>() where T : ModifiableEntity
    {
        var oldCount = Selenium.WindowHandles.Count;

        SearchControl.CreateButton.Find().Click();

        Selenium.Wait(() => Selenium.WindowHandles.Count > oldCount);

        var windowHandles = Selenium.WindowHandles;

        var currentIndex = windowHandles.IndexOf(Selenium.CurrentWindowHandle);

        Selenium.SwitchTo().Window(windowHandles[currentIndex +1]);

        var result = new FramePageProxy<T>(this.Selenium);

        result.OnDisposed += () =>
        {
            Selenium.SwitchTo().Window(windowHandles[currentIndex]);
        };

        return result;
    }


    public void Dispose()
    {
    }

    public void Search()
    {
        this.SearchControl.Search();
    }

    internal SearchPageProxy WaitLoaded()
    {
        this.Selenium.Wait(() => this.SearchControl.SearchButton != null);
        return this;
    }
}
