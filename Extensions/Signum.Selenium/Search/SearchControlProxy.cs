using OpenQA.Selenium;

namespace Signum.Selenium;

public class SearchControlProxy
{
    public WebDriver Selenium { get; private set; }

    public IWebElement Element { get; private set; }

    public object QueryName => QueryLogic.ToQueryName(this.Element.GetDomAttribute("data-query-key")!);

    public FiltersProxy Filters => new FiltersProxy(this.FiltersPanel.Find(), QueryName);
    public ColumnEditorProxy ColumnEditor() => new ColumnEditorProxy(this.Element.FindElement(By.CssSelector(".sf-column-editor")));

    public PaginationSelectorProxy Pagination => new PaginationSelectorProxy(this);
    public ResultTableProxy Results { get; private set; }


    public SearchControlProxy(IWebElement element)
    {
        this.Selenium = element.GetDriver();
        this.Element = element;
        this.Results = new ResultTableProxy(this.Element.FindElement(By.ClassName("sf-scroll-table-container")), this);
    }

    public WebElementLocator SearchButton
    {
        get { return this.Element.WithLocator(By.CssSelector(".sf-query-button.sf-search")); }
    }

    public void Search()
    {
        WaitSearchCompleted(() => SearchButton.Find().Click());
    }

    public void WaitSearchCompleted(Action searchTrigger)
    {
        string counter = this.Element.GetDomAttributeOrThrow("data-search-count");
        searchTrigger();
        WaitSearchCompleted(counter);
    }

    public void WaitInitialSearchCompleted()
    {
        WaitSearchCompleted((string?)null);
    }

    void WaitSearchCompleted(string? counter)
    {
        Selenium.Wait(() =>
         this.Element.GetDomAttribute("data-search-count") != counter
            , () => "button {0} to finish searching".FormatWith(SearchButton));
    }

    public EntityContextMenuProxy SelectedClick()
    {
        this.Element.FindElement(By.CssSelector(".sf-tm-selected")).Click();

        var element = this.Element.WaitElementVisible(By.CssSelector("div.dropdown  > .dropdown-menu"));

        return new EntityContextMenuProxy(this.Results, element);
    }

    public IWebElement WaitContextMenu()
    {
        return Element.WaitElementVisible(By.CssSelector(".sf-context-menu .dropdown-menu"));
    }

    public WebElementLocator ToggleFiltersButton
    {
        get { return this.Element.WithLocator(By.ClassName("sf-filter-button")); }
    }

    public WebElementLocator FiltersPanel
    {
        get { return this.Element.WithLocator(By.ClassName("sf-filters-list")); }
    }

    public void ToggleFilters(bool show)
    {
        ToggleFiltersButton.Find().SafeClick();
        if (show)
            FiltersPanel.WaitVisible();
        else
            FiltersPanel.WaitNoVisible();
    }



    public WebElementLocator ContextualMenu => this.Element.WithLocator(By.ClassName("sf-context-menu"));

    public FilterConditionProxy AddQuickFilter(int rowIndex, string token)
    {
        Results.CellElement(rowIndex, token).Find().ContextClick();

        var menuItem = ContextualMenu.WaitVisible().FindElement(By.CssSelector(".sf-quickfilter-header a"));

        return (FilterConditionProxy)this.Filters.GetNewFilter(() => menuItem.Click());
    }

    public FilterConditionProxy AddQuickFilter(string token)
    {
        Results.HeaderCellElement(token).Find().ContextClick();

        var menuItem = ContextualMenu.WaitVisible().FindElement(By.CssSelector(".sf-quickfilter-header a"));

        return (FilterConditionProxy)this.Filters.GetNewFilter(() => menuItem.Click());
    }

    public FrameModalProxy<T> Create<T>() where T : ModifiableEntity
    {
        var popup = this.CreateButton.Find().CaptureOnClick();

        if (SelectorModalProxy.IsSelector(popup))
            popup = popup.AsSelectorModal().SelectAndCapture<T>();

        return new FrameModalProxy<T>(popup).WaitLoaded();
    }

    public WebElementLocator CreateButton
    {
        get { return this.Element.WithLocator(By.CssSelector(".sf-create")); }
    }

    public bool HasMultiplyMessage
    {
        get { return this.Element.IsElementPresent(By.CssSelector(".sf-td-multiply")); }
    }

    public bool FiltersVisible
    {
        get { return this.FiltersPanel.IsVisible(); }
    }

    public ILineContainer<T> SimpleFilterBuilder<T>() where T : ModifiableEntity
    {
        return new LineContainer<T>(this.Element.FindElement(By.ClassName("simple-filter-builder")));
    }
}
