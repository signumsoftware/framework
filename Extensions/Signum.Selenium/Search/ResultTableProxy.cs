using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

namespace Signum.Selenium;

public class ResultTableProxy
{
    public WebDriver Selenium { get; private set; }

    public IWebElement Element;

    SearchControlProxy SearchControl;

    public ResultTableProxy(IWebElement element, SearchControlProxy searchControl)
    {
        this.Selenium = element.GetDriver();
        this.Element = element;
        this.SearchControl = searchControl;
    }

    public WebElementLocator ResultTableElement
    {
        get { return this.Element.WithLocator(By.CssSelector("table.sf-search-results")); }
    }

    public WebElementLocator RowsLocator
    {
        get { return this.Element.WithLocator(By.CssSelector("table.sf-search-results > tbody > tr")); }
    }

    public List<ResultRowProxy> AllRows()
    {
        return RowsLocator.FindElements().Select(e => new ResultRowProxy(e)).ToList();
    }

    public ResultRowProxy RowElement(int rowIndex)
    {
        return new ResultRowProxy(RowElementLocator(rowIndex).WaitVisible());
    }

    private WebElementLocator RowElementLocator(int rowIndex)
    {
        return this.Element.WithLocator(By.CssSelector("tr[data-row-index='{0}']".FormatWith(rowIndex)));
    }

    public ResultRowProxy RowElement(Lite<IEntity> lite)
    {
        return new ResultRowProxy(RowElementLocator(lite).WaitVisible());
    }

    private WebElementLocator RowElementLocator(Lite<IEntity> lite)
    {
        return this.Element.WithLocator(By.CssSelector("tr[data-entity='{0}']".FormatWith(lite.Key())));
    }

    public List<Lite<IEntity>> SelectedEntities()
    {
        var entities = RowsLocator.FindElements()
            .Where(tr => tr.IsElementPresent(By.CssSelector("input.sf-td-selection:checked")))
            .Select(a => a.GetDomAttribute("data-entity")!);

        if (entities.Any() && entities.All(a => a == null)) //Group By
            throw new InvalidOperationException("Unable to find selected entities, grouping?");

        return entities.NotNull().Select(a => Lite.Parse<IEntity>(a)).ToList();
    }

    public WebElementLocator CellElement(int rowIndex, string token)
    {
        var columnIndex = GetColumnIndex(token);

        return RowElement(rowIndex).CellElement(columnIndex);
    }

    public WebElementLocator CellElement(Lite<IEntity> lite, string token)
    {
        var columnIndex = GetColumnIndex(token);

        return RowElement(lite).CellElement(columnIndex);
    }

    public int GetColumnIndex(string token)
    {
        var tokens = this.GetColumnTokens();

        var index = tokens.IndexOf(token);

        if (index == -1)
            throw new InvalidOperationException("Token {0} not found between {1}".FormatWith(token, tokens.NotNull().CommaAnd()));

        return index;
    }


    public void SelectRow(int rowIndex)
    {
        RowElement(rowIndex).SelectedCheckbox.Find().Click();
    }

    public void SelectRow(params int[] rowIndexes)
    {
        foreach (var index in rowIndexes)
            SelectRow(index);
    }

    public void SelectRow(Lite<IEntity> lite)
    {
        RowElement(lite).SelectedCheckbox.Find().Click();
    }

    public void SelectAllRows()
    {
        SelectRow(0.To(RowsCount()).ToArray());
    }

    public WebElementLocator HeaderElement
    {
        get { return this.Element.WithLocator(By.CssSelector("thead > tr > th")); }
    }

    public string[] GetColumnTokens()
    {
        var ths = this.Element.FindElements(By.CssSelector("thead > tr > th")).ToList();

        return ths.Select(a => a.GetDomAttribute("data-column-name") ?? "").ToArray();
    }

    public WebElementLocator HeaderCellElement(string token)
    {
        return this.HeaderElement.CombineCss("[data-column-name='{0}']".FormatWith(token));
    }

    public ResultTableProxy OrderBy(string token)
    {
        OrderBy(token, OrderType.Ascending);

        return this;
    }

    public ResultTableProxy OrderByDescending(string token)
    {
        OrderBy(token, OrderType.Descending);

        return this;
    }

    public ResultTableProxy ThenBy(string token)
    {
        OrderBy(token, OrderType.Ascending, thenBy: true);

        return this;
    }

    public ResultTableProxy ThenByDescending(string token)
    {
        OrderBy(token, OrderType.Descending, thenBy: true);

        return this;
    }

    private void OrderBy(string token, OrderType orderType, bool thenBy = false)
    {
        do
        {
            SearchControl.WaitSearchCompleted(() =>
            {
                Actions action = new Actions(Selenium);

                if (thenBy)
                    action.KeyDown(Keys.Shift);
                HeaderCellElement(token).Find().Click();
                if (thenBy)
                    action.KeyUp(Keys.Shift);
            });
        }
        while (!HeaderCellElement(token).CombineCss(SortSpan(orderType)).IsPresent());
    }

    public bool HasColumn(string token)
    {
        return HeaderCellElement(token).IsPresent();
    }

    public void RemoveColumn(string token)
    {
        var headerLocator = HeaderCellElement(token);
        headerLocator.Find().ContextClick();

        SearchControl.WaitContextMenu().FindElement(By.CssSelector(".sf-remove-header")).Click();
        headerLocator.WaitNoPresent();
    }

    public WebElementLocator EntityLink(Lite<IEntity> lite)
    {
        var entityIndex = GetColumnIndex("Entity");
        return RowElement(lite).EntityLink(entityIndex);
    }

    public WebElementLocator EntityLink(int rowIndex)
    {
        var entityIndex = GetColumnIndex("Entity");
        return RowElement(rowIndex).EntityLink(entityIndex);
    }

    public FramePageProxy<T> EntityClickInPlace<T>(Lite<T> lite) where T : Entity
    {
        EntityLink(lite).Find().Click();
        return new FramePageProxy<T>(this.Selenium);
    }

    public FramePageProxy<T> EntityClickInPlace<T>(int rowIndex) where T : Entity
    {
        EntityLink(rowIndex).Find().Click();
        return new FramePageProxy<T>(this.Selenium);
    }

    public FrameModalProxy<T> EntityClick<T>(Lite<T> lite) where T : Entity
    {
        var element = EntityLink(lite).Find().CaptureOnClick();
        return new FrameModalProxy<T>(element);
    }

    public void WaitRows(int rows)
    {
        this.Selenium.Wait(() => this.RowsCount() == rows);
    }

    public FrameModalProxy<T> EntityClick<T>(int rowIndex) where T : Entity
    {
        var element = EntityLink(rowIndex).Find().CaptureOnClick();
        return new FrameModalProxy<T>(element);
    }

    public FramePageProxy<T> EntityClickNormalPage<T>(Lite<T> lite) where T : Entity
    {
        EntityLink(lite).Find().Click();
        return new FramePageProxy<T>(this.Element.GetDriver());
    }

    public FramePageProxy<T> EntityClickNormalPage<T>(int rowIndex) where T : Entity
    {
        EntityLink(rowIndex).Find().Click();
        return new FramePageProxy<T>(this.Element.GetDriver());
    }

    public EntityContextMenuProxy EntityContextMenu(int rowIndex, string columnToken = "Entity")
    {
        CellElement(rowIndex, columnToken).Find().ScrollTo().ContextClick();

        var element = this.SearchControl.WaitContextMenu();

        return new EntityContextMenuProxy(this, element);
    }

    public EntityContextMenuProxy EntityContextMenu(Lite<Entity> lite, string columnToken = "Entity")
    {
        CellElement(lite, columnToken).Find().ScrollTo().ContextClick();

        var element = this.SearchControl.WaitContextMenu();

        return new EntityContextMenuProxy(this, element);
    }

    public int RowsCount()
    {
        return this.Element.FindElements(By.CssSelector("tbody > tr[data-entity]")).Count;
    }

    public Lite<Entity> EntityInIndex(int rowIndex)
    {
        var result = this.Element.FindElement(By.CssSelector("tbody > tr:nth-child(" + (rowIndex + 1) + ")")).GetDomAttributeOrThrow("data-entity");

        return Lite.Parse(result);
    }

    public bool IsHeaderMarkedSorted(string token)
    {
        return IsHeaderMarkedSorted(token, OrderType.Ascending) ||
            IsHeaderMarkedSorted(token, OrderType.Descending);
    }


    public bool IsHeaderMarkedSorted(string token, OrderType orderType)
    {
        return HeaderCellElement(token).CombineCss(SortSpan(orderType)).IsPresent();
    }

    private static string SortSpan(OrderType orderType)
    {
        return " span.sf-header-sort." + (orderType == OrderType.Ascending ? "asc" : "desc");
    }

    public bool IsElementInCell(int rowIndex, string token, By locator)
    {
        return CellElement(rowIndex, token).Find().FindElements(locator).Any();
    }

    public ColumnEditorProxy EditColumnName(string token)
    {
        var headerSelector = this.HeaderCellElement(token);
        headerSelector.Find().ContextClick();
        SearchControl.WaitContextMenu().FindElement(By.ClassName("sf-edit-header")).Click();

        return SearchControl.ColumnEditor();
    }

    public ColumnEditorProxy AddColumnBefore(string token)
    {
        var headerSelector = this.HeaderCellElement(token);
        headerSelector.Find().ContextClick();
        SearchControl.WaitContextMenu().FindElement(By.ClassName("sf-insert-header")).Click();

        return SearchControl.ColumnEditor();
    }

    public void RemoveColumnBefore(string token)
    {
        var headerSelector = this.HeaderCellElement(token);
        headerSelector.Find().ContextClick();
        SearchControl.WaitContextMenu().FindElement(By.ClassName("sf-remove-header")).Click();
    }

    public void WaitSuccess(Lite<IEntity> lite) => WaitSuccess(new List<Lite<IEntity>> { lite });
    public void WaitSuccess(List<Lite<IEntity>> lites)
    {
        lites.ForEach(lite => RowElementLocator(lite).CombineCss(".sf-entity-ctxmenu-success").WaitVisible());
    }

    public void WaitNoVisible(Lite<IEntity> lite) => WaitNoVisible(new List<Lite<IEntity>> { lite });

    public void WaitNoVisible(List<Lite<IEntity>> lites)
    {
        lites.ToList().ForEach(lite => RowElementLocator(lite).WaitNoVisible());
    }

    
}

public class ResultRowProxy
{
    public IWebElement RowElement;
    public ResultRowProxy(IWebElement rowElement)
    {
        this.RowElement = rowElement;
    }

    public WebElementLocator SelectedCheckbox => new WebElementLocator(RowElement, By.CssSelector("input.sf-td-selection"));

    public WebElementLocator CellElement(int columnIndex) => new WebElementLocator(RowElement, By.CssSelector("td:nth-child({0})".FormatWith(columnIndex + 1)));

    public WebElementLocator EntityLink(int entityColumnIndex) => CellElement(entityColumnIndex).CombineCss("> a");
}
