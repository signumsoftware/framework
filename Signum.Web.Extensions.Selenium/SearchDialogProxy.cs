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

namespace Signum.Web.Selenium
{
    public class SearchPopupProxy : Popup
    {
        public SearchControlProxy SearchControl { get; private set; }
        public ResultTableProxy Results { get { return SearchControl.Results; } }
        public FiltersProxy Filters { get { return SearchControl.Filters; } }
        public PaginationSelectorProxy Pagination { get { return SearchControl.Pagination; } }

        public SearchPopupProxy(RemoteWebDriver selenium, string prefix)
            : base(selenium, prefix)
        {
            this.SearchControl = new SearchControlProxy(selenium, prefix);
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

        public PopupControl<T> Create<T>() where T : ModifiableEntity
        {
            return SearchControl.Create<T>();
        }

        public PopupControl<T> CreateChoose<T>() where T : ModifiableEntity
        {
            return SearchControl.CreateChoose<T>();
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
            this.SearchControl = new SearchControlProxy(selenium, "");
        }

        public NormalPage<T> Create<T>() where T : ModifiableEntity
        {
            Selenium.FindElement(SearchControl.CreateButtonLocator).Click();

            return new NormalPage<T>(Selenium).WaitLoaded();
        }

        public NormalPage<T> CreateChoose<T>() where T : ModifiableEntity
        {
            Selenium.FindElement(SearchControl.CreateButtonLocator).Click();

            //implementation popup opens
            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, SearchControl.Prefix));

            if (!ChooserPopup.IsChooser(Selenium, SearchControl.Prefix))
                throw new InvalidOperationException("{0} is not a Chooser".FormatWith(Selenium));

            ChooserPopup.ChooseButton(Selenium, SearchControl.Prefix, typeof(T));

            return new NormalPage<T>(Selenium).WaitLoaded();
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
            this.Selenium.WaitElementPresent(this.SearchControl.SearchButtonLocator);
            return this;
        }
    }

    public class SearchControlProxy
    {
        public RemoteWebDriver Selenium { get; private set; }

        public string Prefix { get; private set; }

        public FiltersProxy Filters { get; private set; }
        public PaginationSelectorProxy Pagination { get; private set; }
        public ResultTableProxy Results { get; private set; }


        public SearchControlProxy(RemoteWebDriver selenium, string prefix)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
            this.Pagination = new PaginationSelectorProxy(this);
            this.Results = new ResultTableProxy(this.Selenium, this.PrefixUnderscore, this.WaitSearchCompleted, hasDataEntity: true);
            this.Filters = new FiltersProxy(this.Selenium, PrefixUnderscore);
        }

        public By SearchButtonLocator
        {
            get { return By.CssSelector("#{0}qbSearch".FormatWith(PrefixUnderscore)); }
        }

        public string PrefixUnderscore
        {
            get { return Prefix.HasText() ? Prefix + "_" : null; }
        }

        public void Search()
        {
            WaitSearchCompleted(() => Selenium.FindElement(SearchButtonLocator).Click());
        }

        public void WaitSearchCompleted(Action searchTrigger)
        {
            string counter = (string)Selenium.ExecuteScript("return $('#{0}qbSearch').attr('data-searchcount')".FormatWith(this.PrefixUnderscore));
            searchTrigger();
            WaitSearchCompleted(counter);
        }

        public void WaitInitialSearchCompleted()
        {
            WaitSearchCompleted((string)null);
        }

        void WaitSearchCompleted(string counter)
        {
            By searchButtonDisctinct = SearchButtonLocator.CombineCss((string.IsNullOrEmpty(counter) ? "[data-searchcount]" : "[data-searchcount='{0}']".FormatWith(int.Parse(counter) + 1)));
            Selenium.Wait(() =>
                Selenium.IsElementPresent(searchButtonDisctinct)
                , () => "button {0} to finish searching".FormatWith(SearchButtonLocator));
        }


        public By ToggleFiltersLocator
        {
            get { return By.CssSelector("#{0}sfSearchControl .sf-filters-header".FormatWith(PrefixUnderscore)); }
        }

        public By FiltersPanelLocator
        {
            get { return By.CssSelector("#{0}sfSearchControl .sf-filters".FormatWith(PrefixUnderscore)); }
        }

        public void ToggleFilters(bool show)
        {
            Selenium.FindElement(ToggleFiltersLocator).Click();
            if (show)
                Selenium.WaitElementVisible(FiltersPanelLocator);
            else
                Selenium.WaitElementNotVisible(FiltersPanelLocator);
        }

        public By AddColumnButtonLocator
        {
            get { return By.CssSelector("#{0}btnAddColumn".FormatWith(PrefixUnderscore)); }
        }

        public bool IsAddColumnEnabled
        {
            get
            {
                Selenium.AssertElementPresent(AddColumnButtonLocator);

                return Selenium.IsElementPresent(AddColumnButtonLocator.CombineCss(":not([disabled])"));
            }
        }

        public void AddColumn(string token)
        {
            Filters.QueryTokenBuilder.SelectToken(token);
            Selenium.Wait(() => IsAddColumnEnabled);
            Selenium.FindElement(AddColumnButtonLocator).Click();
            Selenium.WaitElementPresent(Results.HeaderCellLocator(token));
        }


        public FilterOptionProxy AddQuickFilter(int rowIndex, string token)
        {
            var newFilterIndex = Filters.NewFilterIndex();
           
            Selenium.FindElement(Results.CellLocator(rowIndex, token)).ContextClick();

            By quickFilterLocator = By.CssSelector("#sfContextMenu .sf-quickfilter");
            Selenium.WaitElementPresent(quickFilterLocator);
            Selenium.FindElement(quickFilterLocator).Click();

            FilterOptionProxy filter = new FilterOptionProxy(this.Filters, newFilterIndex);
            Selenium.WaitElementPresent(filter.OperationLocator);
            return filter;
        }

        public FilterOptionProxy AddQuickFilter(string token)
        {
            var newFilterIndex = Filters.NewFilterIndex();

            var headerLocator = Results.HeaderCellLocator(token);
            Selenium.FindElement(headerLocator).ContextClick();

            By quickFilterLocator = By.CssSelector("#sfContextMenu .sf-quickfilter-header");
            Selenium.WaitElementPresent(quickFilterLocator);
            Selenium.FindElement(quickFilterLocator).Click();

            FilterOptionProxy filter = new FilterOptionProxy(this.Filters, newFilterIndex);
            Selenium.WaitElementPresent(filter.OperationLocator);
            return filter;
        }


        public By QueryButtonLocator(string id)
        {
            return By.CssSelector("#{0}.sf-query-button".FormatWith(id));
        }

        public void QueryButtonClick(string id)
        {
            Selenium.FindElement(QueryButtonLocator(id)).Click();
        }

        public PopupControl<T> Create<T>() where T : ModifiableEntity
        {
            var prefix = this.PrefixUnderscore + "Temp";

            Selenium.FindElement(CreateButtonLocator).Click();

            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, prefix));

            return new PopupControl<T>(Selenium, prefix);
        }

        public PopupControl<T> CreateChoose<T>() where T : ModifiableEntity
        {
            Selenium.FindElement(CreateButtonLocator).Click();

            //implementation popup opens
            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, Prefix));

            if (!ChooserPopup.IsChooser(Selenium, Prefix))
                throw new InvalidOperationException("{0} is not a Chooser".FormatWith(Selenium));

            ChooserPopup.ChooseButton(Selenium, Prefix, typeof(T));

            return new PopupControl<T>(Selenium, Prefix);
        }

        public By CreateButtonLocator
        {
            get { return QueryButtonLocator(PrefixUnderscore + "qbSearchCreate"); }
        }

        public bool HasMultiplyMessage
        {
            get { return Selenium.IsElementPresent(By.CssSelector("#{0}sfSearchControl .sf-td-multiply".FormatWith(PrefixUnderscore))); }
        }

        public By MenuOptionLocator(string optionId)
        {
            return By.CssSelector("#{0}sfSearchControl a#{1}".FormatWith(PrefixUnderscore, optionId));
        }

        public By MenuOptionLocatorByAttr(string optionLocator)
        {
            return By.CssSelector("#{0}sfSearchControl a[{1}]".FormatWith(PrefixUnderscore, optionLocator));
        }

        public bool FiltersVisible
        {
            get { return this.Selenium.IsElementVisible(this.FiltersPanelLocator); }
        }
    }

    public class FiltersProxy
    {
        public RemoteWebDriver Selenium { get; private set; }
        public string PrefixUnderscore { get; private set; }
        public QueryTokenBuilderProxy QueryTokenBuilder { get; private set; }

        public FiltersProxy(RemoteWebDriver selenium, string PrefixUnderscore)
        {
            this.Selenium = selenium;
            this.PrefixUnderscore = PrefixUnderscore;
            this.QueryTokenBuilder = new QueryTokenBuilderProxy(this.Selenium, PrefixUnderscore + "tokenBuilder_");
        }

        public int NumFilters()
        {
            int result = (int)(long)Selenium.ExecuteScript("return $('#{0}tblFilters tbody tr').length".FormatWith(this.PrefixUnderscore));

            return result;
        }

        public int NewFilterIndex()
        {
            string result = (string)Selenium.ExecuteScript("return $('#{0}tblFilters tbody tr').get().map(function(a){{return parseInt(a.id.substr('{0}trFilter'.length + 1));}}).join()".FormatWith(PrefixUnderscore));

            return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }

        public FilterOptionProxy AddFilter()
        {
            var newFilterIndex = NewFilterIndex();

            Selenium.FindElement(AddFilterButtonLocator).Click();

            FilterOptionProxy filter = new FilterOptionProxy(this, newFilterIndex);
            Selenium.WaitElementPresent(filter.OperationLocator);
            return filter;
        }

        public void AddFilter(string token, FilterOperation operation, object value)
        {
            QueryTokenBuilder.SelectToken(token);

            var filter = AddFilter();
            filter.Operation = operation;
            filter.SetValue(value);
        }

        public By AddFilterButtonLocator
        {
            get { return By.CssSelector("#{0}btnAddFilter".FormatWith(PrefixUnderscore)); }
        }

        public bool IsAddFilterEnabled
        {
            get
            {
                Selenium.AssertElementPresent(AddFilterButtonLocator);

                return Selenium.IsElementPresent(AddFilterButtonLocator.CombineCss(":not([disabled])"));
            }
        }

        public FilterOptionProxy GetFilter(int index)
        {
            return new FilterOptionProxy(this, index);
        }
    }

    public class QueryTokenBuilderProxy
    {
        public RemoteWebDriver Selenium { get; private set; }
        public string PrefixUnderscore { get; private set; }

        public QueryTokenBuilderProxy(RemoteWebDriver selenium, string prefixUnderscore)
        {
            this.Selenium = selenium;
            this.PrefixUnderscore = prefixUnderscore;
        }

        public By TokenLocator(int tokenIndex, string previousToken, bool isEnd)
        {
            var result = By.CssSelector("#{0}{1}_{2}".FormatWith(PrefixUnderscore, !isEnd ? "ddlTokens" : "ddlTokensEnd", tokenIndex));

            result = result.CombineCss("[data-parenttoken='" + previousToken + "']");

            return result;
        }

        public void SelectToken(string token)
        {
            string[] parts = token.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                var prev = parts.Take(i).ToString(".");

                var tokenLocator = TokenLocator(i, prev, isEnd: false);

                Selenium.WaitElementPresent(tokenLocator);

                Selenium.FindElement(tokenLocator).SelectElement().SelectByValue(parts[i]);
            }

            Selenium.Wait(() =>
            {
                var tokenLocator = TokenLocator(parts.Length, token, isEnd: false);
                if (Selenium.IsElementPresent(tokenLocator))
                {
                    Selenium.FindElement(tokenLocator).SelectElement().SelectByValue("");
                    return true;
                }

                if (Selenium.IsElementPresent(TokenLocator(parts.Length, token, isEnd: true)))
                    return true;

                return false;
            });
        }


    }

    public class ResultTableProxy
    {
        public RemoteWebDriver Selenium { get; private set; }

        public string PrefixUnderscore;

        public Action<Action> WaitSearchCompleted;

        public bool HasDataEntity;

        public ResultTableProxy(RemoteWebDriver selenium, string prefixUndescore, Action<Action> waitSearchCompleted, bool hasDataEntity)
        {
            this.Selenium = selenium;
            this.PrefixUnderscore = prefixUndescore;
            this.WaitSearchCompleted = waitSearchCompleted;
            this.HasDataEntity = hasDataEntity;
        }

        public By ResultTableLocator
        {
            get { return By.CssSelector("#{0}tblResults".FormatWith(PrefixUnderscore)); }
        }

        public By RowsLocator
        {
            get { return By.CssSelector("#{0}tblResults > tbody > tr".FormatWith(PrefixUnderscore)); }
        }

        public By RowLocator(int rowIndex)
        {
            return RowsLocator.CombineCss(":nth-child({0})".FormatWith(rowIndex + 1));
        }

        public By RowLocator(Lite<IEntity> lite)
        {
            return RowsLocator.CombineCss("[data-entity='{0}']".FormatWith(lite.Key()));
        }

        public By CellLocator(int rowIndex, string token)
        {
            var index = GetColumnIndex(token);

            return RowLocator(rowIndex).CombineCss("> td:nth-child({0})".FormatWith(index + 1));
        }

        public By CellLocator(Lite<IEntity> lite, string token)
        {
            var index = GetColumnIndex(token);

            return RowLocator(lite).CombineCss("> td:nth-child({0})".FormatWith(index + 1));
        }

        private int GetColumnIndex(string token)
        {
            var tokens = this.GetColumnTokens();

            var index = tokens.IndexOf(token);

            if (index == -1)
                throw new InvalidOperationException("Token {0} not found between {1}".FormatWith(token, tokens.CommaAnd()));
            return index;
        }

        public By RowSelectorLocator(int rowIndex)
        {
            return By.Id("{0}rowSelection_{1}".FormatWith(PrefixUnderscore, rowIndex));
        }

        public void SelectRow(int rowIndex)
        {
            Selenium.FindElement(RowSelectorLocator(rowIndex)).Click();
        }

        public void SelectRow(params int[] rowIndexes)
        {
            foreach (var index in rowIndexes)
                SelectRow(index);
        }

        public void SelectRow(Lite<IEntity> lite)
        {
            Selenium.FindElement(RowLocator(lite).CombineCss(" .sf-td-selection")).Click();
        }

        public bool HasFooter
        {
            get { return Selenium.IsElementPresent(By.CssSelector("#{0}tblResults > tbody > tr.sf-search-footer".FormatWith(PrefixUnderscore))); }
        }

        public By HeaderLocator
        {
            get { return By.CssSelector("#{0}tblResults > thead > tr > th".FormatWith(PrefixUnderscore)); }
        }

        public string[] GetColumnTokens()
        {
            var array = (string)Selenium.ExecuteScript("return $('#" + PrefixUnderscore + "tblResults > thead > tr > th')" +
                ".toArray().map(function(e){ return  $(e).hasClass('sf-th-entity')? 'Entity' : $(e).attr('data-column-name'); }).join(',')");

            return array.Split(',');
        }

        public By HeaderCellLocator(string token)
        {
            return HeaderLocator.CombineCss("[data-column-name='{0}']".FormatWith(token));
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
                WaitSearchCompleted(() =>
                {
                    if (thenBy)
                        Selenium.Keyboard.PressKey(Keys.Shift);
                    Selenium.FindElement(HeaderCellLocator(token)).Click();
                    if (thenBy)
                        Selenium.Keyboard.ReleaseKey(Keys.Shift);
                });
            }
            while (!Selenium.IsElementPresent(HeaderCellLocator(token).CombineCss(SortSpan(orderType))));
        }

        public bool HasColumn(string token)
        {
            return Selenium.IsElementPresent(HeaderCellLocator(token));
        }

        public void RemoveColumn(string token)
        {
            By headerSelector = HeaderCellLocator(token);
            Selenium.FindElement(headerSelector).ContextClick();
            Selenium.FindElement(By.CssSelector("#sfContextMenu .sf-remove-header")).Click();
            Selenium.WaitElementNotPresent(headerSelector);
        }

        public By EntityLinkLocator(Lite<IEntity> lite, bool allowSelection = true)
        {
            return RowLocator(lite).CombineCss(" > td:nth-child({0}) > a".FormatWith(allowSelection ? 2 : 1));
        }

        public By EntityLinkLocator(int rowIndex, bool allowSelection = true)
        {
            return RowLocator(rowIndex).CombineCss(" > td:nth-child({0}) > a".FormatWith(allowSelection ? 2 : 1));
        }

        public NormalPage<T> EntityClick<T>(Lite<T> lite, bool allowSelection = true) where T : Entity
        {
            Selenium.FindElement(EntityLinkLocator(lite, allowSelection)).Click();
            return new NormalPage<T>(Selenium).WaitLoaded();
        }

        public NormalPage<T> EntityClick<T>(int rowIndex, bool allowSelection = true) where T : Entity
        {
            Selenium.FindElement(EntityLinkLocator(rowIndex, allowSelection)).Click();
            return new NormalPage<T>(Selenium).WaitLoaded();
        }

        public EntityContextMenuProxy EntityContextMenu(int rowIndex)
        {
            Selenium.FindElement(CellLocator(rowIndex, "Entity")).ContextClick();

            EntityContextMenuProxy ctx = new EntityContextMenuProxy(this, isContext: true);

            ctx.WaitNotLoading();

            return ctx;
        }

     

        public EntityContextMenuProxy SelectedClick()
        {
            Selenium.FindElement(By.CssSelector("#{0}sfSearchControl .sf-tm-selected".FormatWith(PrefixUnderscore))).Click();

            EntityContextMenuProxy ctx = new EntityContextMenuProxy(this, isContext: false);

            ctx.WaitNotLoading();

            return ctx;
        }

        public int RowsCount()
        {
            return (int)(long)Selenium.ExecuteScript("return $('#{0}tblResults > tbody > tr{1}').length".FormatWith(PrefixUnderscore, (HasDataEntity ? "[data-entity]" : null)));
        }

        public Lite<Entity> EntityInIndex(int index)
        {
            var result = (string)Selenium.ExecuteScript("return $('{0}').data('entity')".FormatWith(RowLocator(index).CssSelector()));

            return Lite.Parse(result);
        }

        public bool IsHeaderMarkedSorted(string token)
        {
            return IsHeaderMarkedSorted(token, OrderType.Ascending) ||
                IsHeaderMarkedSorted(token, OrderType.Descending);
        }


        public bool IsHeaderMarkedSorted(string token, OrderType orderType)
        {
            return Selenium.IsElementPresent(HeaderCellLocator(token).CombineCss(SortSpan(orderType)));
        }

        private static string SortSpan(OrderType orderType)
        {
            return " span.sf-header-sort." + (orderType == OrderType.Ascending ? "asc" : "desc");
        }

        public bool IsElementInCell(int rowIndex, string token, By locator)
        {
            return Selenium.FindElement(CellLocator(rowIndex, token)).FindElements(locator).Any();
        }

        public void EditColumnName(string token, string newName)
        {
            By headerSelector = this.HeaderCellLocator(token);
            Selenium.FindElement(headerSelector).ContextClick();
            Selenium.FindElement(By.CssSelector("#sfContextMenu .sf-edit-header")).Click();

            using (var popup = new Popup(Selenium, this.PrefixUnderscore + "newName"))
            {
                Selenium.WaitElementPresent(popup.PopupLocator);
                Selenium.FindElement(popup.PopupLocator.CombineCss(" input[type=text]")).SafeSendKeys(newName);
                popup.OkWaitClosed();
            }

            Selenium.Wait(() => Selenium.FindElement(headerSelector).FindElements(By.CssSelector("span")).Any(s => s.Text == newName));
        }
    }

    public class EntityContextMenuProxy
    {
        ResultTableProxy resultTable;
        bool IsContext;
        public EntityContextMenuProxy(ResultTableProxy resultTable, bool isContext)
        {
            this.resultTable = resultTable;
            this.IsContext = isContext;
        }

        public By EntityContextMenuLocator
        {
            get
            {
                if (IsContext)
                    return By.CssSelector("#sfContextMenu");
                else
                    return By.CssSelector("#{0}btnSelectedDropDown".FormatWith(resultTable.PrefixUnderscore));
            }
        }

        public void QuickLinkClick(string title)
        {
            resultTable.Selenium.FindElement(EntityContextMenuLocator.CombineCss(" li.sf-quick-link[data-name='{0}'] > a".FormatWith(title))).Click();
        }

        public SearchPopupProxy QuickLinkClickSearch(string title)
        {
            QuickLinkClick(title);
            var result = new SearchPopupProxy(resultTable.Selenium, resultTable.PrefixUnderscore + "New");
            resultTable.Selenium.WaitElementPresent(result.PopupLocator);
            result.SearchControl.WaitInitialSearchCompleted();
            return result;
        }

        public void ExecuteClick(IOperationSymbolContainer symbolContainer, bool consumeConfirmation = false)
        {
            ExecuteClick(symbolContainer.Symbol, consumeConfirmation);
        }

        public void ExecuteClick(OperationSymbol operationSymbol, bool consumeConfirmation = false)
        {
            MenuClick(operationSymbol.KeyWeb());
            if (consumeConfirmation)
                this.resultTable.Selenium.ConsumeAlert();

            resultTable.Selenium.WaitElementNotVisible(EntityContextMenuLocator);
        }

        public void DeleteClick(IOperationSymbolContainer symbolContainer, bool consumeConfirmation = true)
        {
            DeleteClick(symbolContainer.Symbol);
        }

        public void DeleteClick(OperationSymbol operationSymbol, bool consumeConfirmation = true)
        {
            MenuClick(operationSymbol.KeyWeb());
            if (consumeConfirmation)
                this.resultTable.Selenium.ConsumeAlert();
        }

        public PopupControl<ProcessEntity> DeleteProcessClick(IOperationSymbolContainer symbolContainer)
        {
            return DeleteProcessClick(symbolContainer.Symbol);
        }

        public PopupControl<ProcessEntity> DeleteProcessClick(OperationSymbol operationSymbol)
        {
            MenuClick(operationSymbol.KeyWeb());
            resultTable.Selenium.ConsumeAlert();

            var result = new PopupControl<ProcessEntity>(this.resultTable.Selenium, "New");
            result.Selenium.WaitElementPresent(result.PopupLocator);
            return result;
        }


        public void MenuClick(string itemId)
        {
            var loc = MenuItemLocator(itemId);
            //resultTable.Selenium.FindElement(loc).MouseUp();
            resultTable.Selenium.FindElement(loc).Click();
        }

        public By MenuItemLocator(string itemId)
        {
            return EntityContextMenuLocator.CombineCss(" li a#{0}".FormatWith(itemId));
        }

        public bool IsDisabled(string itemId)
        {
            return resultTable.Selenium.IsElementPresent(MenuItemLocator(itemId).CombineCss("[disabled]"));
        }

        public bool IsDisabled(IOperationSymbolContainer symbol)
        {
            return IsDisabled(symbol.Symbol.KeyWeb());
        }

        public PopupControl<T> MenuClickPopup<T>(string itemId, string prefix = "New")
            where T : Entity
        {
            MenuClick(itemId);
            //resultTable.Selenium.WaitElementDisapear(EntityContextMenuLocator);
            var result = new PopupControl<T>(this.resultTable.Selenium, prefix);
            result.Selenium.WaitElementPresent(result.PopupLocator);
            return result;
        }

        public PopupControl<T> MenuClickPopup<T>(IOperationSymbolContainer contanier, string prefix = "New")
            where T : Entity
        {
            return MenuClickPopup<T>(contanier.Symbol.KeyWeb(), prefix);
        }

        public NormalPage<T> MenuClickNormalPage<T>(IOperationSymbolContainer contanier) where T : Entity
        {
            return MenuClickNormalPage<T>(contanier.Symbol.KeyWeb());
        }

        private NormalPage<T> MenuClickNormalPage<T>(string itemId) where T : Entity
        {
            MenuClick(itemId);
            var result = new NormalPage<T>(this.resultTable.Selenium).WaitLoaded();
            return result;
        }

        public void WaitNotLoading()
        {
            this.resultTable.Selenium.Wait(() =>
                this.resultTable.Selenium.FindElement(this.EntityContextMenuLocator)
                    .FindElements(By.CssSelector("li")).Any(a => !a.FindElements(By.CssSelector(".sf-tm-selected-loading")).Any()));
        }
    }

    public class PaginationSelectorProxy
    {
        public SearchControlProxy SearchControl { get; private set; }

        public PaginationSelectorProxy(SearchControlProxy seachControl)
        {
            this.SearchControl = seachControl;
        }

        public By ElementsPerPageLocator
        {
            get { return By.CssSelector("#{0}sfElems".FormatWith(SearchControl.PrefixUnderscore)); }
        }

        public void SetElementsPerPage(int elementPerPage)
        {
            SearchControl.WaitSearchCompleted(() =>
            {
                var combo = ElementsPerPageLocator;
                SearchControl.Selenium.FindElement(combo).SelectElement().SelectByValue(elementPerPage.ToString());
                //SearchControl.Selenium.FireEvent(combo, "change");
            });
        }

        public By PaginationModeLocator
        {
            get { return By.CssSelector("#{0}sfPaginationMode".FormatWith(SearchControl.Prefix)); }
        }

        public void SetPaginationMode(PaginationMode mode)
        {
            var combo = PaginationModeLocator;
            SearchControl.Selenium.FindElement(combo).SelectElement().SelectByValue(mode.ToString());
            //SearchControl.Selenium.FireEvent(combo, "change");
        }
    }

    public class FilterOptionProxy
    {
        public FiltersProxy Filters { get; private set; }
        public int FilterIndex { get; private set; }

        public FilterOptionProxy(FiltersProxy filters, int index)
        {
            this.Filters = filters;
            this.FilterIndex = index;
        }

        public By OperationLocator
        {
            get { return By.CssSelector("#{0}ddlSelector_{1}".FormatWith(Filters.PrefixUnderscore, FilterIndex)); }
        }

        public FilterOperation Operation
        {
            get { return Filters.Selenium.FindElement(OperationLocator).SelectElement().SelectedOption.GetAttribute("value").ToEnum<FilterOperation>(); }
            set { Filters.Selenium.FindElement(OperationLocator).SelectElement().SelectByValue(value.ToString()); }
        }

        public By DeleteButtonLocator
        {
            get { return By.CssSelector("#{0}btnDelete_{1}".FormatWith(Filters.PrefixUnderscore, FilterIndex)); }
        }

        public void Delete()
        {
            Filters.Selenium.FindElement(DeleteButtonLocator).Click();
        }

        public ValueLineProxy ValueLine()
        {
            return new ValueLineProxy(Filters.Selenium, "{0}value_{1}".FormatWith(Filters.PrefixUnderscore, FilterIndex), null);
        }

        public EntityLineProxy EntityLine()
        {
            return new EntityLineProxy(Filters.Selenium, "{0}value_{1}".FormatWith(Filters.PrefixUnderscore, FilterIndex), null);
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
}
