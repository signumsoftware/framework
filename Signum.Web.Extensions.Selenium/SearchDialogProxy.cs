using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Selenium;
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

        public SearchPopupProxy(ISelenium selenium, string prefix)
            : base(selenium, prefix)
        {
            this.SearchControl = new SearchControlProxy(selenium, prefix);
        }

        public void SelectLite(Lite<IIdentifiable> lite)
        {
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

        public void SelectById(int id)
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
        public ISelenium Selenium { get; private set; }
        public SearchControlProxy SearchControl { get; private set; }
        public ResultTableProxy Results { get { return SearchControl.Results; } }
        public FiltersProxy Filters { get { return SearchControl.Filters; } }
        public PaginationSelectorProxy Pagination { get { return SearchControl.Pagination; } }

        public SearchPageProxy(ISelenium selenium)
        {
            this.Selenium = selenium;
            this.SearchControl = new SearchControlProxy(selenium, "");
        }

        public NormalPage<T> Create<T>() where T : ModifiableEntity
        {
            Selenium.Click(SearchControl.CreateButtonLocator);

            Selenium.WaitForPageToLoad();

            return new NormalPage<T>(Selenium);
        }

        public NormalPage<T> CreateChoose<T>() where T : ModifiableEntity
        {
            Selenium.Click(SearchControl.CreateButtonLocator);

            //implementation popup opens
            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, SearchControl.Prefix));

            if (!ChooserPopup.IsChooser(Selenium, SearchControl.Prefix))
                throw new InvalidOperationException("{0} is not a Chooser".Formato(Selenium));

            ChooserPopup.ChooseButton(Selenium, SearchControl.Prefix, typeof(T));

            Selenium.WaitForPageToLoad();

            return new NormalPage<T>(Selenium);
        }

        public void Dispose()
        {
        }

        public void Search()
        {
            this.SearchControl.Search();
        }
    }

    public class SearchControlProxy
    {
        public ISelenium Selenium { get; private set; }

        public string Prefix { get; private set; }

        public FiltersProxy Filters { get; private set; }
        public PaginationSelectorProxy Pagination { get; private set; }
        public ResultTableProxy Results { get; private set; }


        public SearchControlProxy(ISelenium selenium, string prefix)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
            this.Pagination = new PaginationSelectorProxy(this);
            this.Results = new ResultTableProxy(this.Selenium, this.PrefixUnderscore, this.WaitSearchCompleted, hasDataEntity: true);
            this.Filters = new FiltersProxy(this.Selenium, PrefixUnderscore);
        }

        public string SearchButtonLocator
        {
            get { return "jq=#{0}qbSearch".Formato(PrefixUnderscore); }
        }

        public string PrefixUnderscore
        {
            get { return Prefix.HasText() ? Prefix + "_" : null; }
        }

        public void Search()
        {
            WaitSearchCompleted(() => Selenium.Click(SearchButtonLocator));
        }

        public void WaitSearchCompleted(Action searchTrigger)
        {
            string counter = Selenium.GetEval("window.$('#{0}qbSearch').attr('data-searchcount')".Formato(this.PrefixUnderscore));
            searchTrigger();
            WaitSearchCompleted(counter);
        }

        public void WaitInitialSearchCompleted()
        {
            WaitSearchCompleted("null");
        }

        void WaitSearchCompleted(string counter)
        {
            var searchButtonDisctinct = SearchButtonLocator + (counter == "null" ? "[data-searchcount]" : "[data-searchcount!={0}]".Formato(counter));
            Selenium.Wait(() =>
                Selenium.IsElementPresent(searchButtonDisctinct)
                , () => "button {0} to finish searching".Formato(SearchButtonLocator));
        }


        public string ToggleFiltersLocator
        {
            get { return "jq=#{0}sfSearchControl .sf-filters-header".Formato(PrefixUnderscore); }
        }

        public string FiltersPanelLocator
        {
            get { return "jq=#{0}sfSearchControl .sf-filters".Formato(PrefixUnderscore); }
        }

        public void ToggleFilters(bool show)
        {
            Selenium.Click(ToggleFiltersLocator);
            Selenium.WaitElementPresent(FiltersPanelLocator + (show ? ":visible" : ":hidden"));
        }



        public string AddColumnButtonLocator
        {
            get { return "jq=#{0}btnAddColumn".Formato(PrefixUnderscore); }
        }

        public bool IsAddColumnEnabled
        {
            get
            {
                Selenium.AssertElementPresent(AddColumnButtonLocator);

                return Selenium.IsElementPresent(AddColumnButtonLocator + ":not([disabled])");
            }
        }

        public void AddColumn(string token)
        {
            Filters.QueryTokenBuilder.SelectToken(token);
            Selenium.Wait(() => IsAddColumnEnabled);
            Selenium.Click(AddColumnButtonLocator);
            Selenium.WaitElementPresent(Results.HeaderCellLocator(token));
        }


        public FilterOptionProxy AddQuickFilter(int rowIndex, string token)
        {
            var newFilterIndex = Filters.NewFilterIndex();

            string cellLocator = Results.CellLocator(rowIndex, token);
            Selenium.ContextMenuAt(cellLocator, "0,0");

            string quickFilterLocator = "jq=#sfContextMenu .sf-quickfilter";
            Selenium.WaitElementPresent(quickFilterLocator);
            Selenium.Click(quickFilterLocator);

            FilterOptionProxy filter = new FilterOptionProxy(this.Filters, newFilterIndex);
            Selenium.WaitElementPresent(filter.OperationLocator);
            return filter;
        }

        public FilterOptionProxy AddQuickFilter(string token)
        {
            var newFilterIndex = Filters.NewFilterIndex();

            string cellLocator = Results.HeaderCellLocator(token);
            Selenium.ContextMenuAt(cellLocator, "0,0");

            string quickFilterLocator = "jq=#sfContextMenu .sf-quickfilter-header".Formato(cellLocator);
            Selenium.WaitElementPresent(quickFilterLocator);
            Selenium.Click(quickFilterLocator);

            FilterOptionProxy filter = new FilterOptionProxy(this.Filters, newFilterIndex);
            Selenium.WaitElementPresent(filter.OperationLocator);
            return filter;
        }


        public string QueryButtonLocator(string id)
        {
            return "jq=#{0}.sf-query-button".Formato(id);
        }

        public void QueryButtonClick(string id)
        {
            Selenium.MouseUp(QueryButtonLocator(id));
        }

        public PopupControl<T> Create<T>() where T : ModifiableEntity
        {
            var prefix = this.PrefixUnderscore + "Temp";

            Selenium.Click(CreateButtonLocator);

            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, prefix));

            return new PopupControl<T>(Selenium, prefix);
        }

        public PopupControl<T> CreateChoose<T>() where T : ModifiableEntity
        {
            Selenium.Click(CreateButtonLocator);

            //implementation popup opens
            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, Prefix));

            if (!ChooserPopup.IsChooser(Selenium, Prefix))
                throw new InvalidOperationException("{0} is not a Chooser".Formato(Selenium));

            ChooserPopup.ChooseButton(Selenium, Prefix, typeof(T));

            return new PopupControl<T>(Selenium, Prefix);
        }

        public string CreateButtonLocator
        {
            get { return QueryButtonLocator(PrefixUnderscore + "qbSearchCreate"); }
        }

        public bool HasMultiplyMessage
        {
            get { return Selenium.IsElementPresent("jq=#{0}sfSearchControl .sf-td-multiply".Formato(PrefixUnderscore)); }
        }

        public string MenuOptionLocator(string optionId)
        {
            return "jq=#{0}sfSearchControl a#{1}".Formato(PrefixUnderscore, optionId);
        }

        public string MenuOptionLocatorByAttr(string optionLocator)
        {
            return "jq=#{0}sfSearchControl a[{1}]".Formato(PrefixUnderscore, optionLocator);
        }
    }

    public class FiltersProxy
    {
        public ISelenium Selenium { get; private set; }
        public string PrefixUnderscore { get; private set; }
        public QueryTokenBuilderProxy QueryTokenBuilder { get; private set; }

        public FiltersProxy(ISelenium selenium, string PrefixUnderscore)
        {
            this.Selenium = selenium;
            this.PrefixUnderscore = PrefixUnderscore;
            this.QueryTokenBuilder = new QueryTokenBuilderProxy(this.Selenium, PrefixUnderscore + "tokenBuilder_");
        }

        public int NumFilters()
        {
            string result = Selenium.GetEval("window.$('#{0}tblFilters tbody tr').length".Formato(this.PrefixUnderscore));

            return int.Parse(result);
        }

        public int NewFilterIndex()
        {
            string result = Selenium.GetEval("window.$('#{0}tblFilters tbody tr').get().map(function(a){{return parseInt(a.id.substr('{0}trFilter'.length + 1));}}).join()".Formato(PrefixUnderscore));

            return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }

        public FilterOptionProxy AddFilter()
        {
            var newFilterIndex = NewFilterIndex();

            Selenium.Click(AddFilterButtonLocator);

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

        public string AddFilterButtonLocator
        {
            get { return "jq=#{0}btnAddFilter".Formato(PrefixUnderscore); }
        }

        public bool IsAddFilterEnabled
        {
            get
            {
                Selenium.AssertElementPresent(AddFilterButtonLocator);

                return Selenium.IsElementPresent(AddFilterButtonLocator + ":not([disabled])");
            }
        }

        public FilterOptionProxy GetFilter(int index)
        {
            return new FilterOptionProxy(this, index);
        }
    }

    public class QueryTokenBuilderProxy
    {
        public ISelenium Selenium { get; private set; }
        public string PrefixUnderscore { get; private set; }

        public QueryTokenBuilderProxy(ISelenium selenium, string prefixUnderscore)
        {
            this.Selenium = selenium;
            this.PrefixUnderscore = prefixUnderscore;
        }

        public string TokenLocator(int tokenIndex, string previousToken, bool isEnd)
        {
            var result = "jq=#{0}{1}_{2}".Formato(PrefixUnderscore, !isEnd ? "ddlTokens" : "ddlTokensEnd", tokenIndex);

            result += "[data-parenttoken='" + previousToken + "']";

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

                Selenium.Select(tokenLocator, "value=" + parts[i]);
            }

            Selenium.Wait(() =>
            {
                var tokenLocator = TokenLocator(parts.Length, token, isEnd: false);
                if (Selenium.IsElementPresent(tokenLocator))
                {
                    Selenium.Select(tokenLocator, "value=");
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
        public ISelenium Selenium { get; private set; }

        public string PrefixUnderscore;

        public Action<Action> WaitSearchCompleted;

        public bool HasDataEntity;

        public ResultTableProxy(ISelenium selenium, string prefixUndescore, Action<Action> waitSearchCompleted, bool hasDataEntity)
        {
            this.Selenium = selenium;
            this.PrefixUnderscore = prefixUndescore;
            this.WaitSearchCompleted = waitSearchCompleted;
            this.HasDataEntity = hasDataEntity;
        }

        public string ResultTableLocator
        {
            get { return "jq=#{0}tblResults".Formato(PrefixUnderscore); }
        }

        public string RowsLocator
        {
            get { return "jq=#{0}tblResults > tbody > tr".Formato(PrefixUnderscore); }
        }

        public string RowLocator(int rowIndex)
        {
            return RowsLocator + ":nth-child({0})".Formato(rowIndex + 1);
        }

        public string RowLocator(Lite<IIdentifiable> lite)
        {
            return "{0}[data-entity='{1}']".Formato(RowsLocator, lite.Key());
        }

        public string CellLocator(int rowIndex, string token)
        {
            var tokens = this.GetColumnTokens();

            var index = tokens.IndexOf(token);

            if (index == -1)
                throw new InvalidOperationException("Token {0} not found between {1}".Formato(token, tokens.CommaAnd()));

            return RowLocator(rowIndex) + "> td:nth-child({0})".Formato(index + 1);
        }

        public string RowSelectorLocator(int rowIndex)
        {
            return "{0}rowSelection_{1}".Formato(PrefixUnderscore, rowIndex);
        }

        public void SelectRow(int rowIndex)
        {
            Selenium.Click(RowSelectorLocator(rowIndex));
        }

        public void SelectRow(params int[] rowIndexes)
        {
            foreach (var index in rowIndexes)
                SelectRow(index);
        }

        public void SelectRow(Lite<IIdentifiable> lite)
        {
            Selenium.Click(RowLocator(lite) + " .sf-td-selection");
        }

        public bool HasFooter
        {
            get { return Selenium.IsElementPresent("jq=#{0}tblResults > tbody > tr.sf-search-footer".Formato(PrefixUnderscore)); }
        }

        public string HeaderLocator
        {
            get { return "jq=#{0}tblResults > thead > tr > th".Formato(PrefixUnderscore); }
        }

        public string[] GetColumnTokens()
        {
            var array = Selenium.GetEval("window.$('#" + PrefixUnderscore + "tblResults > thead > tr > th')" +
                ".toArray().map(function(e){ return  $(e).hasClass('sf-th-entity')? 'Entity' : $(e).attr('data-column-name'); }).join(',')");

            return array.Split(',');
        }

        public string HeaderCellLocator(string token)
        {
            return HeaderLocator + "[data-column-name='{0}']".Formato(token);
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
                        Selenium.ShiftKeyDown();
                    Selenium.Click(HeaderCellLocator(token));
                    if (thenBy)
                        Selenium.ShiftKeyUp();
                });
            }
            while (!Selenium.IsElementPresent(HeaderCellLocator(token) + SortSpan(orderType)));
        }

        public bool HasColumn(string token)
        {
            return Selenium.IsElementPresent(HeaderCellLocator(token));
        }

        public void RemoveColumn(string token)
        {
            string headerSelector = HeaderCellLocator(token);
            Selenium.ContextMenuAt(headerSelector, "0,0");
            Selenium.Click("jq=#sfContextMenu .sf-remove-header");
            Selenium.WaitElementDisapear(headerSelector);
        }

        public string EntityLinkLocator(Lite<IIdentifiable> lite, bool allowSelection = true)
        {
            return RowLocator(lite) + " > td:nth-child({0}) > a".Formato(allowSelection ? 2 : 1);
        }

        public string EntityLinkLocator(int rowIndex, bool allowSelection = true)
        {
            return RowLocator(rowIndex) + " > td:nth-child({0}) > a".Formato(allowSelection ? 2 : 1);
        }

        public NormalPage<T> EntityClick<T>(Lite<T> lite, bool allowSelection = true) where T : IdentifiableEntity
        {
            Selenium.Click(EntityLinkLocator(lite, allowSelection));
            Selenium.WaitForPageToLoad();
            return new NormalPage<T>(Selenium);
        }

        public NormalPage<T> EntityClick<T>(int rowIndex, bool allowSelection = true) where T : IdentifiableEntity
        {
            Selenium.Click(EntityLinkLocator(rowIndex, allowSelection));
            Selenium.WaitForPageToLoad();
            return new NormalPage<T>(Selenium);
        }

        public EntityContextMenuProxy EntityContextMenu(int rowIndex)
        {
            Selenium.ContextMenuAt(CellLocator(rowIndex, "Entity"), "0,0");

            EntityContextMenuProxy ctx = new EntityContextMenuProxy(this, isContext: true);

            Selenium.WaitElementPresent(ctx.EntityContextMenuLocator + ":not(:has(.sf-tm-selected-loading))");

            return ctx;
        }

        public EntityContextMenuProxy SelectedClick()
        {
            Selenium.Click("jq=#{0}sfSearchControl .sf-tm-selected".Formato(PrefixUnderscore));

            EntityContextMenuProxy ctx = new EntityContextMenuProxy(this, isContext: false);

            Selenium.WaitElementPresent(ctx.EntityContextMenuLocator + ":not(:has(.sf-tm-selected-loading))");

            return ctx;
        }

        public int RowsCount()
        {
            string result = Selenium.GetEval("window.$('#{0}tblResults > tbody > tr{1}').length".Formato(PrefixUnderscore, (HasDataEntity ? "[data-entity]" : null)));

            int num = int.Parse(result);

            return num;
        }

        public Lite<IdentifiableEntity> EntityInIndex(int index)
        {
            var result = Selenium.GetEval("window.$('{0}').data('entity')".Formato(RowLocator(index).RemoveStart(3)));

            return Lite.Parse(result);
        }

        public bool IsHeaderMarkedSorted(string token)
        {
            return IsHeaderMarkedSorted(token, OrderType.Ascending) ||
                IsHeaderMarkedSorted(token, OrderType.Descending);
        }


        public bool IsHeaderMarkedSorted(string token, OrderType orderType)
        {
            return Selenium.IsElementPresent(HeaderCellLocator(token) + SortSpan(orderType));
        }

        private static string SortSpan(OrderType orderType)
        {
            return " span.sf-header-sort." + (orderType == OrderType.Ascending ? "asc" : "desc");
        }

        public bool IsElementInCell(int rowIndex, string token, string selector)
        {
            return Selenium.IsElementPresent(CellLocator(rowIndex, token) + " " + selector);
        }

        public void EditColumnName(string token, string newName)
        {
            string headerSelector = this.HeaderCellLocator(token);
            Selenium.ContextMenuAt(headerSelector, "0,0");
            Selenium.Click("jq=#sfContextMenu .sf-edit-header");

            using (var popup = new Popup(Selenium, this.PrefixUnderscore + "newName"))
            {
                Selenium.WaitElementPresent(popup.PopupVisibleLocator);
                Selenium.Type(popup.PopupVisibleLocator + " input:text", newName);
                popup.OkWaitClosed();
            }

            Selenium.WaitElementPresent("{0}:contains('{1}')".Formato(headerSelector, newName));
        }


        //public void MoveAfter(string srcToken, string targetToken)
        //{
        //    MoveColumn(srcToken, targetToken, after: true );
        //}

        //public void MoveBefore(string srcToken, string targetToken)
        //{
        //    MoveColumn(srcToken, targetToken, after: false);
        //}

        //void MoveColumn(string srcToken, string targetToken, bool after)
        //{
        //    string srcLocator = HeaderCellLocator(srcToken);
        //    string targetLocator = HeaderCellLocator(targetToken);

        //    decimal srcX = Selenium.GetElementPositionLeft(srcLocator);
        //    decimal targetX = Selenium.GetElementPositionLeft(targetLocator);
        //    decimal targetWidth = Selenium.GetElementWidth(targetLocator);

        //    decimal dx = (targetX - srcX) + (after ? (targetWidth - 5) : 5);

        //    Selenium.DragAndDrop(srcLocator, dx.ToString("+00;-00;+00") + ",+0");
        //}
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

        public string EntityContextMenuLocator
        {
            get
            {
                if (IsContext)
                    return "jq=#sfContextMenu:visible";
                else
                    return "jq=#{0}btnSelectedDropDown:visible".Formato(resultTable.PrefixUnderscore);
            }
        }

        public void QuickLinkClick(string title)
        {
            resultTable.Selenium.Click("{0} li.sf-quick-link[data-name='{1}'] > a".Formato(EntityContextMenuLocator, title));
        }

        public SearchPopupProxy QuickLinkClickSearch(string title)
        {
            QuickLinkClick(title);
            var result = new SearchPopupProxy(resultTable.Selenium, resultTable.PrefixUnderscore + "New");
            resultTable.Selenium.WaitElementPresent(result.PopupVisibleLocator);
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
                this.resultTable.Selenium.ConsumeConfirmation();

            resultTable.Selenium.WaitElementDisapear(EntityContextMenuLocator);
        }

        public void DeleteClick(IOperationSymbolContainer symbolContainer, bool consumeConfirmation = true)
        {
            DeleteClick(symbolContainer.Symbol);
        }

        public void DeleteClick(OperationSymbol operationSymbol, bool consumeConfirmation = true)
        {
            MenuClick(operationSymbol.KeyWeb());
            if (consumeConfirmation)
                this.resultTable.Selenium.ConsumeConfirmation();
        }

        public PopupControl<ProcessDN> DeleteProcessClick(IOperationSymbolContainer symbolContainer)
        {
            return DeleteProcessClick(symbolContainer.Symbol);
        }

        public PopupControl<ProcessDN> DeleteProcessClick(OperationSymbol operationSymbol)
        {
            MenuClick(operationSymbol.KeyWeb());
            resultTable.Selenium.ConsumeConfirmation();

            var result = new PopupControl<ProcessDN>(this.resultTable.Selenium, "New");
            result.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            return result;
        }


        public void MenuClick(string itemId)
        {
            var loc = MenuItemLocator(itemId);
            resultTable.Selenium.MouseUp(loc);
            resultTable.Selenium.Click(loc);
        }

        public string MenuItemLocator(string itemId)
        {
            return "{0} li a#{1}".Formato(EntityContextMenuLocator, itemId);
        }

        public bool IsDisabled(string itemId)
        {
            return resultTable.Selenium.IsElementPresent(MenuItemLocator(itemId) + "[disabled]");
        }

        public bool IsDisabled(IOperationSymbolContainer symbol)
        {
            return IsDisabled(symbol.Symbol.KeyWeb());
        }

        public PopupControl<T> MenuClickPopup<T>(string itemId, string prefix = "New")
            where T : IdentifiableEntity
        {
            MenuClick(itemId);
            //resultTable.Selenium.WaitElementDisapear(EntityContextMenuLocator);
            var result = new PopupControl<T>(this.resultTable.Selenium, prefix);
            result.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            return result;
        }

        public PopupControl<T> MenuClickPopup<T>(IOperationSymbolContainer contanier, string prefix = "New")
            where T : IdentifiableEntity
        {
            return MenuClickPopup<T>(contanier.Symbol.KeyWeb(), prefix);
        }

        public NormalPage<T> MenuClickNormalPage<T>(IOperationSymbolContainer contanier) where T : IdentifiableEntity
        {
            return MenuClickNormalPage<T>(contanier.Symbol.KeyWeb());
        }

        private NormalPage<T> MenuClickNormalPage<T>(string itemId) where T : IdentifiableEntity
        {
            MenuClick(itemId);
            resultTable.Selenium.WaitForPageToLoad();
            var result = new NormalPage<T>(this.resultTable.Selenium);
            return result;
        }
    }

    public class PaginationSelectorProxy
    {
        public SearchControlProxy SearchControl { get; private set; }

        public PaginationSelectorProxy(SearchControlProxy seachControl)
        {
            this.SearchControl = seachControl;
        }

        public string ElementsPerPageLocator
        {
            get { return "jq=#{0}sfElems".Formato(SearchControl.PrefixUnderscore); }
        }

        public void SetElementsPerPage(int elementPerPage)
        {
            SearchControl.WaitSearchCompleted(() =>
            {
                var combo = ElementsPerPageLocator;
                SearchControl.Selenium.Select(combo, "value=" + elementPerPage.ToString());
                SearchControl.Selenium.FireEvent(combo, "change");
            });
        }

        public string PaginationModeLocator
        {
            get { return "jq=#{0}sfPaginationMode".Formato(SearchControl.Prefix); }
        }

        public void SetPaginationMode(PaginationMode mode)
        {
            var combo = PaginationModeLocator;
            SearchControl.Selenium.Select(combo, "value=" + mode.ToString());
            SearchControl.Selenium.FireEvent(combo, "change");
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

        public string OperationLocator
        {
            get { return "jq=#{0}ddlSelector_{1}".Formato(Filters.PrefixUnderscore, FilterIndex); }
        }

        public FilterOperation Operation
        {
            get { return Filters.Selenium.GetValue(OperationLocator).ToEnum<FilterOperation>(); }
            set { Filters.Selenium.Select(OperationLocator, "value=" + value.ToString()); }
        }

        public string DeleteButtonLocator
        {
            get { return "jq=#{0}btnDelete_{1}".Formato(Filters.PrefixUnderscore, FilterIndex); }
        }

        public void Delete()
        {
            Filters.Selenium.Click(DeleteButtonLocator);
        }

        public ValueLineProxy ValueLine()
        {
            return new ValueLineProxy(Filters.Selenium, "{0}value_{1}".Formato(Filters.PrefixUnderscore, FilterIndex), null);
        }

        public EntityLineProxy EntityLine()
        {
            return new EntityLineProxy(Filters.Selenium, "{0}value_{1}".Formato(Filters.PrefixUnderscore, FilterIndex), null);
        }

        internal void SetValue(object value)
        {
            if (value == null)
                return; //Hack

            if (value is Lite<IdentifiableEntity>)
                EntityLine().LiteValue = (Lite<IdentifiableEntity>)value;
            else if (value is IdentifiableEntity)
                EntityLine().LiteValue = ((IdentifiableEntity)value).ToLite();
            else
                ValueLine().StringValue = value.ToString();
        }
    }
}
