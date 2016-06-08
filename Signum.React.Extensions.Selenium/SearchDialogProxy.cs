using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Processes;
using Signum.Utilities;
using Signum.React.Selenium;

namespace Signum.React.Selenium
{
    public class SearchPopupProxy : Popup
    {
        public SearchControlProxy SearchControl { get; private set; }
        public ResultTableProxy Results { get { return SearchControl.Results; } }
        public FiltersProxy Filters { get { return SearchControl.Filters; } }
        public PaginationSelectorProxy Pagination { get { return SearchControl.Pagination; } }

        public SearchPopupProxy(IWebElement element)
            : base(element)
        {
            this.SearchControl = new SearchControlProxy(element);
        }

        public void SelectLite(Lite<IEntity> lite)
        {
            if (!this.SearchControl.FiltersVisible)
                this.SearchControl.ToggleFilters(true);

            this.SearchControl.Filters.AddFilter("Id", FilterOperation.EqualTo, lite.Id);

            this.SearchControl.Search();

            this.SearchControl.Results.SelectRow(lite);

            this.OkWaitClosed();

            this.Dispose();
        }

        public void SelectByPosition(int rowIndex)
        {
            this.SearchControl.Search();

            this.SearchControl.Results.SelectRow(rowIndex);

            this.OkWaitClosed();

            this.Dispose();
        }

        public void SelectByPositionOrderById(int rowIndex)
        {
            this.Results.OrderBy("Id");

            this.SearchControl.Results.SelectRow(rowIndex);

            this.OkWaitClosed();

            this.Dispose();
        }

        public void SelectById(PrimaryKey id)
        {
            this.SearchControl.Filters.AddFilter("Id", FilterOperation.EqualTo, id);
            this.SearchControl.Search();
            this.Results.SelectRow(0);

            this.OkWaitClosed();

            this.Dispose();
        }

        public void SelectByPosition(params int[] rowIndexes)
        {
            this.SearchControl.Search();

            foreach (var index in rowIndexes)
                this.SearchControl.Results.SelectRow(index);

            this.OkWaitClosed();

            this.Dispose();
        }

        public PopupFrame<T> Create<T>() where T : ModifiableEntity
        {
            return SearchControl.Create<T>();
        }

        public void Search()
        {
            this.SearchControl.Search();
        }
    }

    public class SearchPageProxy : IDisposable
    {
        public RemoteWebDriver Selenium { get; private set; }
        public SearchControlProxy SearchControl { get; private set; }
        public ResultTableProxy Results { get { return SearchControl.Results; } }
        public FiltersProxy Filters { get { return SearchControl.Filters; } }
        public PaginationSelectorProxy Pagination { get { return SearchControl.Pagination; } }

        public SearchPageProxy(RemoteWebDriver selenium)
        {
            this.Selenium = selenium;
            this.SearchControl = new SearchControlProxy(selenium.WaitElementVisible(By.ClassName("sf-search-control")));
        }

        public PopupFrame<T> Create<T>() where T : ModifiableEntity
        {
            var popup = SearchControl.CreateButton.Find().CaptureOnClick();

            if (SelectorModal.IsSelector(popup))
                popup = popup.GetDriver().CapturePopup(() => SelectorModal.Select(popup, typeof(T)));

            return new PopupFrame<T>(popup);
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

    public class SearchControlProxy
    {
        public RemoteWebDriver Selenium { get; private set; }

        public IWebElement Element { get; private set; }

        public FiltersProxy Filters => new FiltersProxy(this.Element.FindElement(By.CssSelector("div.sf-filters")));
        public ColumnEditorProxy ColumnEditor() => new ColumnEditorProxy(this.Element.FindElement(By.CssSelector(".sf-column-editor")));

        public PaginationSelectorProxy Pagination => new PaginationSelectorProxy(this);
        public ResultTableProxy Results { get; private set; }


        public SearchControlProxy(IWebElement element)
        {
            this.Selenium = element.GetDriver();
            this.Element = element;
            this.Results = new ResultTableProxy(this.Element.FindElement(By.ClassName("sf-search-results-container")), this);
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
            string counter = this.Element.GetAttribute("data-search-count");
            searchTrigger();
            WaitSearchCompleted(counter);
        }

        public void WaitInitialSearchCompleted()
        {
            WaitSearchCompleted((string)null);
        }

        void WaitSearchCompleted(string counter)
        {
            Selenium.Wait(() =>
             this.Element.GetAttribute("data-search-count") != counter
                , () => "button {0} to finish searching".FormatWith(SearchButton));
        }

        public EntityContextMenuProxy SelectedClick()
        {
            this.Element.FindElement(By.CssSelector("ul.sf-tm-selected")).Click();

            var element = this.Element.WaitElementVisible(By.CssSelector("div.dropdown  > ul.dropdown-menu"));

            return new EntityContextMenuProxy(this.Results, element);
        }

        public IWebElement WaitContextMenu()
        {
            return Element.WaitElementVisible(By.CssSelector("ul.sf-context-menu"));
        }

        public WebElementLocator ToggleFiltersButton
        {
            get { return this.Element.WithLocator(By.ClassName("sf-filters-header")); }
        }

        public WebElementLocator FiltersPanel
        {
            get { return this.Element.WithLocator(By.ClassName("sf-filters")); }
        }

        public void ToggleFilters(bool show)
        {
            ToggleFiltersButton.Find().Click();
            if (show)
                FiltersPanel.WaitVisible();
            else
                FiltersPanel.WaitNoVisible();
        }



        public WebElementLocator ContextualMenu => this.Element.WithLocator(By.ClassName("sf-context-menu"));

        public FilterOptionProxy AddQuickFilter(int rowIndex, string token)
        {
            Results.CellElement(rowIndex, token).Find().ContextClick();

            var menuItem = ContextualMenu.WaitVisible().FindElement(By.CssSelector(".sf-quickfilter-header a"));

            return this.Filters.GetNewFilter(() => menuItem.Click());
        }

        public FilterOptionProxy AddQuickFilter(string token)
        {
            Results.HeaderCellElement(token).Find().ContextClick();

            var menuItem = ContextualMenu.WaitVisible().FindElement(By.CssSelector(".sf-quickfilter-header a"));

            return this.Filters.GetNewFilter(() => menuItem.Click());
        }

        public PopupFrame<T> Create<T>() where T : ModifiableEntity
        {
            var popup = this.CreateButton.Find().CaptureOnClick();

            if (SelectorModal.IsSelector(popup))
                popup = popup.GetDriver().CapturePopup(() => SelectorModal.Select(popup, typeof(T)));

            return new PopupFrame<T>(popup).WaitLoaded();
        }

        public WebElementLocator CreateButton
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-query-button.sf-create")); }
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

    public class FiltersProxy
    {
        public IWebElement Element { get; private set; }

        public FiltersProxy(IWebElement element)
        {
            this.Element = element;
        }

        public IEnumerable<IWebElement> Filters()
        {
            return Element.FindElements(By.CssSelector("table.sf-filter-table > tbody > tr"));
        }

        public FilterOptionProxy GetNewFilter(Action action)
        {
            var oldFilters = this.Filters();
            action();
            var newFilter = this.Element.GetDriver().Wait(() => this.Filters().Except(oldFilters).SingleOrDefault(), () => "new filter to appear");

            return new FilterOptionProxy(newFilter);
        }

        public WebElementLocator AddFilterButton
        {
            get { return this.Element.WithLocator(By.ClassName("sf-line-button sf-create")); }
        }

        public FilterOptionProxy AddFilter()
        {
            return GetNewFilter(() => this.AddFilterButton.Find().Click());
        }

        public void AddFilter(string token, FilterOperation operation, object value)
        {
            var fo = this.AddFilter();
            fo.QueryToken.SelectToken(token);
            fo.Operation = operation;
            fo.SetValue(value);
        }

        public bool IsAddFilterEnabled
        {
            get { return this.AddFilterButton.CombineCss(":not([disabled])").IsPresent(); }
        }

        public FilterOptionProxy GetFilter(int index)
        {
            return new FilterOptionProxy(this.Filters().ElementAt(index));
        }


    }


    public class ResultTableProxy
    {
        public RemoteWebDriver Selenium { get; private set; }

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

        public WebElementLocator RowsElement
        {
            get { return this.Element.WithLocator(By.CssSelector("table.sf-search-results > tbody > tr")); }
        }

        public WebElementLocator RowElement(int rowIndex)
        {
            return this.Element.WithLocator(By.CssSelector("tr[data-row-index={0}]".FormatWith(rowIndex + 1)));
        }

        public WebElementLocator RowElement(Lite<IEntity> lite)
        {
            return this.Element.WithLocator(By.CssSelector("tr[data-entity='{0}']".FormatWith(lite.Key())));
        }

        public WebElementLocator CellElement(int rowIndex, string token)
        {
            var index = GetColumnIndex(token);

            return RowElement(rowIndex).CombineCss("> td:nth-child({0})".FormatWith(index));
        }

        public WebElementLocator CellElement(Lite<IEntity> lite, string token)
        {
            var index = GetColumnIndex(token);

            return RowElement(lite).CombineCss("> td:nth-child({0})".FormatWith(index));
        }

        private int GetColumnIndex(string token)
        {
            var tokens = this.GetColumnTokens();

            var index = tokens.IndexOf(token);

            if (index == -1)
                throw new InvalidOperationException("Token {0} not found between {1}".FormatWith(token, tokens.NotNull().CommaAnd()));
            return index;
        }

        public WebElementLocator RowSelectorElement(int rowIndex)
        {
            return this.Element.WithLocator(By.CssSelector("tr[data-row-index={0}] .sf-td-selection".FormatWith(rowIndex)));
        }

        public void SelectRow(int rowIndex)
        {
            RowSelectorElement(rowIndex).Find().Click();
        }

        public void SelectRow(params int[] rowIndexes)
        {
            foreach (var index in rowIndexes)
                SelectRow(index);
        }

        public void SelectRow(Lite<IEntity> lite)
        {
            RowElement(lite).CombineCss(" .sf-td-selection").Find().Click();
        }

        public WebElementLocator HeaderElement
        {
            get { return this.Element.WithLocator(By.CssSelector("thead > tr > th")); }
        }

        public string[] GetColumnTokens()
        {
            var ths = this.Element.FindElements(By.CssSelector("thead > tr > th")).ToList();

            return ths.Select(a => a.GetAttribute("data-column-name")).ToArray();
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
                    if (thenBy)
                        Selenium.Keyboard.PressKey(Keys.Shift);
                    HeaderCellElement(token).Find().Click();
                    if (thenBy)
                        Selenium.Keyboard.ReleaseKey(Keys.Shift);
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
            return RowElement(lite).CombineCss(" > td:nth-child({0}) > a");
        }

        public WebElementLocator EntityLink(int rowIndex)
        {
            return RowElement(rowIndex).CombineCss(" > td:nth-child({0}) > a");
        }


        public PopupFrame<T> EntityClick<T>(Lite<T> lite) where T : Entity
        {
            var element = EntityLink(lite).Find().CaptureOnClick();
            return new PopupFrame<T>(element);
        }

        public PopupFrame<T> EntityClick<T>(int rowIndex) where T : Entity
        {
            var element = EntityLink(rowIndex).Find().CaptureOnClick();
            return new PopupFrame<T>(element);
        }

        public PageFrame<T> EntityClickNormalPage<T>(Lite<T> lite) where T : Entity
        {
            EntityLink(lite).Find().Click();
            return new PageFrame<T>(this.Element.GetDriver());
        }

        public PageFrame<T> EntityClickNormalPage<T>(int rowIndex) where T : Entity
        {
            EntityLink(rowIndex).Find().Click();
            return new PageFrame<T>(this.Element.GetDriver());
        }

        public EntityContextMenuProxy EntityContextMenu(int rowIndex, string columnToken = "Entity")
        {
            CellElement(rowIndex, columnToken).Find().ContextClick();

            var element = this.SearchControl.WaitContextMenu();

            return new EntityContextMenuProxy(this, element);
        }

        public EntityContextMenuProxy EntityContextMenu(Lite<Entity> lite, string columnToken = "Entity")
        {
            CellElement(lite, columnToken).Find().ContextClick();

            var element = this.SearchControl.WaitContextMenu();

            return new EntityContextMenuProxy(this, element);
        }

        public int RowsCount()
        {
            return this.Element.FindElements(By.CssSelector("tbody > tr")).Count;
        }

        public Lite<Entity> EntityInIndex(int index)
        {
            var result = this.Element.FindElement(By.CssSelector("tbody > tr:nth-child(" + index + ")")).GetAttribute("data-entity");

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

        public void WaitActiveSuccess()
        {
            RowsElement.CombineCss(".sf-entity-ctxmenu-success").WaitVisible();
        }
    }

    public class ColumnEditorProxy
    {
        public IWebElement Element;

        public ColumnEditorProxy(IWebElement element)
        {
            this.Element = element;
        }


        public void Close()
        {
            this.Element.FindElement(By.ClassName("button.close")).Click();
        }

        public QueryTokenBuilderProxy QueryToken => new QueryTokenBuilderProxy(this.Element.FindElement(By.ClassName("sf-query-token-builder")));
        public IWebElement Name => this.Element.FindElement(By.ClassName("input.form-control"));
    }

    public class EntityContextMenuProxy
    {
        ResultTableProxy ResultTable;
        public IWebElement Element { get; private set; }
        public EntityContextMenuProxy(ResultTableProxy resultTable, IWebElement element)
        {
            this.ResultTable = resultTable;
            this.Element = element;
        }


        public WebElementLocator QuickLink(string name)
        {
            return this.Element.WithLocator(By.CssSelector("a[data-name='{0}']".FormatWith(name)));
        }

        public SearchPopupProxy QuickLinkClickSearch(string name)
        {
            var a = QuickLink(name).WaitPresent();
            var popup = a.CaptureOnClick();
            return new SearchPopupProxy(popup);
        }

        public void ExecuteClick(IOperationSymbolContainer symbolContainer, bool consumeConfirmation = false)
        {
            Operation(symbolContainer).Find().Click();
            if (consumeConfirmation)
                this.ResultTable.Selenium.ConsumeAlert();

            ResultTable.WaitActiveSuccess();
        }

        public void DeleteClick(IOperationSymbolContainer symbolContainer, bool consumeConfirmation = true)
        {
            Operation(symbolContainer).Find().Click();
            if (consumeConfirmation)
                this.ResultTable.Selenium.ConsumeAlert();

            ResultTable.WaitActiveSuccess();
        }

        public PopupFrame<ProcessEntity> DeleteProcessClick(IOperationSymbolContainer operationSymbol)
        {
            Operation(operationSymbol).Find();

            var popup = this.Element.GetDriver().CapturePopup(() =>
            ResultTable.Selenium.ConsumeAlert());

            return new PopupFrame<ProcessEntity>(popup).WaitLoaded();
        }

        public WebElementLocator Operation(IOperationSymbolContainer symbolContainer)
        {
            return this.Element.WithLocator(By.CssSelector("a[data-operation=\'{0}']".FormatWith(symbolContainer.Symbol.Key)));
        }

        public bool OperationIsDisabled(IOperationSymbolContainer symbolContainer)
        {
            return Operation(symbolContainer).Find().GetAttribute("disabled").HasText();
        }

        public PopupFrame<T> OperationClickPopup<T>(IOperationSymbolContainer symbolContainer)
            where T : Entity
        {
            var popup = Operation(symbolContainer).Find().CaptureOnClick();
            return new PopupFrame<T>(popup);
        }

        private PageFrame<T> MenuClickNormalPage<T>(IOperationSymbolContainer contanier) where T : Entity
        {
            OperationIsDisabled(contanier);
            var result = new PageFrame<T>(this.ResultTable.Selenium);
            return result;
        }

        public void WaitNotLoading()
        {
            this.Element.WaitElementNotPresent(By.CssSelector("li.sf-tm-selected-loading"));
        }
    }

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

    public class FilterOptionProxy
    {
        public IWebElement Element;

        public FilterOptionProxy(IWebElement element)
        {
            this.Element = element;
        }

        public WebElementLocator DeleteButton
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-line-button.sf-remove")); }
        }

        public QueryTokenBuilderProxy QueryToken
        {
            get { return new QueryTokenBuilderProxy(this.Element.FindElement(By.ClassName("sf-query-token-builder"))); }
        }

        public WebElementLocator OperationElement
        {
            get { return this.Element.WithLocator(By.CssSelector("td.sf-filter-operation select")); }
        }

        public WebElementLocator ValueElement
        {
            get { return this.Element.WithLocator(By.CssSelector("tr.sf-filter-value *")); }
        }

        public FilterOperation Operation
        {
            get { return OperationElement.Find().SelectElement().SelectedOption.GetAttribute("value").ToEnum<FilterOperation>(); }
            set { OperationElement.Find().SelectElement().SelectByValue(value.ToString()); }
        }

        public void Delete()
        {
            DeleteButton.Find().Click();
        }

        public ValueLineProxy ValueLine()
        {
            return new ValueLineProxy(this.Element, null);
        }

        public EntityLineProxy EntityLine()
        {
            return new EntityLineProxy(this.Element, null);
        }

        internal void SetValue(object value)
        {
            if (value == null)
                return; //Hack

            if (value is Lite<Entity>)
                EntityLine().LiteValue = (Lite<Entity>)value;
            else if (value is Entity)
                EntityLine().LiteValue = ((Entity)value).ToLite();
            else
                ValueLine().StringValue = value.ToString();
        }
    }


    public class QueryTokenBuilderProxy
    {
        public IWebElement Element { get; private set; }

        public QueryTokenBuilderProxy(IWebElement element)
        {
            this.Element = element;
        }

        public WebElementLocator TokenElement(int tokenIndex)
        {
            return this.Element.WithLocator(By.CssSelector(".sf-query-token-part:nth-child(2)"));
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
