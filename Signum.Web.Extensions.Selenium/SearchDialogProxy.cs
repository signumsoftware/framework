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

        public SearchPopupProxy(ISelenium selenium, string prefix) : base(selenium, prefix)
        {
            this.SearchControl = new SearchControlProxy(selenium, prefix);
        }

        public void SelectLite(Lite<IIdentifiable> lite)
        {
            this.SearchControl.Filters.AddFilter("Id", FilterOperation.EqualTo, lite.Id);

            this.SearchControl.Search();

            this.SearchControl.Results.SelectRow(lite);

            this.OkWaitClosed();
        }

        public void SelectByPosition(int rowIndex)
        {   
            this.SearchControl.Search();

            this.SearchControl.Results.SelectRow(rowIndex);

            this.OkWaitClosed();
        }

        public void SelectByPositionOrderById(int rowIndex)
        {
            this.Results.OrderBy("Id");

            this.SearchControl.Results.SelectRow(rowIndex);

            this.OkWaitClosed();
        }

        public void SelectByPosition(params int[] rowIndexes)
        {
            this.SearchControl.Search();

            foreach (var index in rowIndexes)
                this.SearchControl.Results.SelectRow(index);

            this.OkWaitClosed();
        }

        public PopupControl<T> Create<T>() where T : ModifiableEntity
        {
            Selenium.Click(SearchControl.CreateButtonLocator);

            Selenium.WaitForPageToLoad();

            return new PopupControl<T>(Selenium, Prefix);
        }

        public PopupControl<T> CreateChoose<T>() where T : ModifiableEntity
        {
            Selenium.Click(SearchControl.CreateButtonLocator);

            //implementation popup opens
            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, SearchControl.Prefix));

            if (Popup.IsChooser(Selenium, SearchControl.Prefix))
                throw new InvalidOperationException("{0} is not a Chooser".Formato(Selenium));

            Selenium.Click(TypeLogic.GetCleanName(typeof(T)));

            Selenium.WaitForPageToLoad();

            return new PopupControl<T>(Selenium, Prefix);
        }

        public void Search()
        {
            this.SearchControl.Search();
        }
    }

    public class SearchPageProxy :IDisposable
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

            if (!Popup.IsChooser(Selenium, SearchControl.Prefix))
                throw new InvalidOperationException("{0} is not a Chooser".Formato(Selenium));

            Selenium.Click(TypeLogic.GetCleanName(typeof(T)));

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
            Selenium.Click(SearchButtonLocator);
            WaitSearchCompleted();
        }

        public void WaitSearchCompleted()
        {
            var searchButton = SearchButtonLocator;
            Selenium.Wait(() =>
                Selenium.IsElementPresent(searchButton) &&
                !Selenium.IsElementPresent("{0}.sf-searching".Formato(searchButton)), () => "button {0} to stop searching".Formato(searchButton));
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

                return Selenium.IsElementPresent(AddColumnButtonLocator + ":not(.ui-button-disabled)");
            }
        }

        public void AddColumn(string token)
        {
            Filters.QueryTokenBuilder.SelectToken(token);
            Selenium.Wait(() => IsAddColumnEnabled);
            Selenium.Click(AddColumnButtonLocator);
            Selenium.WaitElementPresent(Results.HeaderCellLocator(token));
        }

        public FilterOptionProxy AddQuickFilter(int rowIndex, int columnIndex)
        {
            var newFilterIndex = Filters.NewFilterIndex();

            string cellLocator = Results.CellLocator(rowIndex, columnIndex);
            Selenium.ContextMenu(cellLocator);

            string quickFilterLocator = "{0} .sf-quickfilter > span".Formato(cellLocator);
            Selenium.WaitElementPresent(quickFilterLocator);
            Selenium.Click(quickFilterLocator);

            FilterOptionProxy filter = new FilterOptionProxy(this.Filters, newFilterIndex);
            Selenium.WaitElementPresent(filter.OperationLocator);
            return filter;
        }

        public FilterOptionProxy AddQuickFilter(int columnIndex)
        {
            var newFilterIndex = Filters.NewFilterIndex();

            string cellLocator = Results.HeaderCellLocator(columnIndex);
            Selenium.ContextMenu(cellLocator);

            string quickFilterLocator = "{0} .sf-quickfilter-header > span".Formato(cellLocator);
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
            Selenium.Click(QueryButtonLocator(id));
        }

        public string CreateButtonLocator
        {
            get { return QueryButtonLocator(PrefixUnderscore + "qbSearchCreate"); }
        }


        public string MenuOptionLocator(string menuId, string optionId)
        {
            return "jq=#{0}.sf-dropdown ul.sf-menu-button li.ui-menu-item a.sf-query-button#{1}".Formato(menuId, optionId);
        }

        public string MenuOptionLocatorByAttr(string menuId, string optionLocator)
        {
            return "jq=#{0}.sf-dropdown ul.sf-menu-button li.ui-menu-item a.sf-query-button[{1}]".Formato(menuId, optionLocator);
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
            this.QueryTokenBuilder = new QueryTokenBuilderProxy(this.Selenium, PrefixUnderscore);
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

                return Selenium.IsElementPresent(AddFilterButtonLocator + ":not(.ui-button-disabled)");
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

        public string TokenLocator(int tokenIndex)
        {
            return "{0}ddlTokens_{1}".Formato(PrefixUnderscore, tokenIndex);
        }

        public void WaitTokenCharged(int tokenIndex)
        {
            Selenium.WaitElementPresent("{0}ddlTokens_{1}".Formato(PrefixUnderscore, tokenIndex));
        }

        public void SelectToken(string token, bool waitForLast = false)
        {
            string[] parts = token.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                Selenium.Select(TokenLocator(i), "value=" + parts[i]);

                if (i < parts.Length - 1 || waitForLast)
                    WaitTokenCharged(i + 1);
            }

            if (Selenium.IsElementPresent(TokenLocator(parts.Length)))
                Selenium.Select(TokenLocator(parts.Length), "value=");
        }
    }

    public class ResultTableProxy
    {  
        public ISelenium Selenium { get; private set; }

        public string PrefixUnderscore;

        public Action WaitSearchCompleted;

        public bool HasDataEntity;

        public ResultTableProxy(ISelenium selenium, string prefixUndescore, Action waitSearchCompleted, bool hasDataEntity)
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
            int index = rowIndex + (HasMultiplyMessage ? 1 : 0);

            return RowsLocator + ":nth-child({0})".Formato(index + 1); 
        }

        public string RowLocator(Lite<IIdentifiable> lite)
        {
            return "{0}[data-entity='{1}']".Formato(RowsLocator, lite.Key());
        }

        public string CellLocator(int rowIndex, int columnIndex)
        {
            return RowLocator(rowIndex) + "> td:nth-child({0})".Formato(columnIndex + 1);
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

        public bool HasMultiplyMessage
        {
            get { return Selenium.IsElementPresent("jq=#{0}tblResults > tbody > tr.sf-tr-multiply".Formato(PrefixUnderscore)); }
        }

        public bool HasFooter
        {
            get { return Selenium.IsElementPresent("jq=#{0}tblResults > tbody > tr.sf-search-footer".Formato(PrefixUnderscore)); }
        }

        public string HeaderLocator
        {
            get { return "jq=#{0}tblResults > thead > tr > th".Formato(PrefixUnderscore); }
        }

        public string HeaderCellLocator(int columnIndex)
        {
            return HeaderLocator + ":nth-child({0})".Formato(columnIndex + 1);
        }

     
        public ResultTableProxy OrderBy(int columnIndex)
        {
            OrderBy(columnIndex, OrderType.Ascending);

            return this;
        }

        public ResultTableProxy OrderByDescending(int columnIndex)
        {
            OrderBy(columnIndex, OrderType.Descending);

            return this;
        }

        public ResultTableProxy ThenBy(int columnIndex)
        {
            OrderBy(columnIndex, OrderType.Ascending, thenBy: true);

            return this;
        }

        public ResultTableProxy ThenByDescending(int columnIndex)
        {
            OrderBy(columnIndex, OrderType.Descending, thenBy: true);

            return this;
        }

        private void OrderBy(int columnIndex, OrderType orderType, bool thenBy = false)
        {
            do
            {
                if(thenBy)
                    Selenium.ShiftKeyDown();
                Selenium.Click(HeaderCellLocator(columnIndex));
                if (thenBy)
                    Selenium.ShiftKeyUp();

                WaitSearchCompleted();
            }
            while (!Selenium.IsElementPresent(HeaderCellLocator(columnIndex) + "." + CssClass(orderType)));
        }

        public string HeaderCellLocator(string token)
        {
            return HeaderLocator + ":has(input[type=hidden][value='{0}'])".Formato(token);
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

        private void OrderBy(string token, OrderType  orderType, bool thenBy = false)
        {
            do
            {
                if (thenBy)
                    Selenium.ShiftKeyDown();
                Selenium.Click(HeaderCellLocator(token));
                if (thenBy)
                    Selenium.ShiftKeyUp();

                WaitSearchCompleted();
            }
            while (!Selenium.IsElementPresent(HeaderCellLocator(token) + "." + CssClass(orderType)));
        }

        public bool HasColumn(string token)
        {
            return Selenium.IsElementPresent("{0} > :hidden[value='{1}']".Formato(HeaderLocator, token));
        }

        public void RemoveColumn(int columnIndex)
        {
            int numberOfColumnsBeforeDeleting = -1 ;
            string lastHeaderSelector = HeaderCellLocator(numberOfColumnsBeforeDeleting);
            string headerSelector = HeaderCellLocator(columnIndex);
            Selenium.ContextMenu(headerSelector);
            Selenium.Click("{0} .sf-remove-column > span".Formato(headerSelector));
            Selenium.WaitElementDisapear(lastHeaderSelector);
        }

        public string EntityLinkLocator(Lite<IIdentifiable> lite, bool allowMultiple = true)
        {
            return RowLocator(lite) + " > td:nth-child({0}) > a".Formato(allowMultiple ? 2 : 1);
        }

        public string EntityLinkLocator(int rowIndex, bool allowMultiple = true)
        {
            return RowLocator(rowIndex) + " > td:nth-child({0}) > a".Formato(allowMultiple ? 2 : 1);
        }

        public NormalPage<T> EntityClick<T>(Lite<T> lite, bool allowMultiple = true) where T : IdentifiableEntity
        {
            Selenium.Click(EntityLinkLocator(lite, allowMultiple));
            Selenium.WaitForPageToLoad();
            return new NormalPage<T>(Selenium);
        }

        public NormalPage<T> EntityClick<T>(int rowIndex, bool allowMultiple = true) where T : IdentifiableEntity
        {
            Selenium.Click(EntityLinkLocator(rowIndex, allowMultiple));
            Selenium.WaitForPageToLoad();
            return new NormalPage<T>(Selenium);
        }

        public EntityContextMenuProxy EntityContextMenu(int rowIndex)
        {
            Selenium.ContextMenu(CellLocator(rowIndex, 1));

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

        public bool IsHeaderMarkedSorted(int columnIndex)
        {
            return IsHeaderMarkedSorted(columnIndex, OrderType.Ascending) ||
                IsHeaderMarkedSorted(columnIndex, OrderType.Descending);
        }
        

        public bool IsHeaderMarkedSorted(int columnIndex, OrderType orderType)
        {
            return Selenium.IsElementPresent("{0}.{1}".Formato(
                HeaderCellLocator(columnIndex),
                CssClass(orderType)));
        }

        private static string CssClass(OrderType orderType)
        {
            return orderType == OrderType.Ascending ? "sf-header-sort-down" : "sf-header-sort-up";
        }

        public bool IsElementInCell(int rowIndex, int columnIndex, string selector)
        {
            return Selenium.IsElementPresent(CellLocator(rowIndex, columnIndex) + " " + selector);
        }

        public void EditColumnName(int columnIndex, string newName)
        {
            string headerSelector = this.HeaderCellLocator(columnIndex);
            Selenium.ContextMenu(headerSelector);
            Selenium.Click("{0} .sf-edit-column > span".Formato(headerSelector));

            using (var popup = new Popup(Selenium, this.PrefixUnderscore + "newName"))
            {   
                Selenium.WaitElementPresent(popup.PopupVisibleLocator);
                Selenium.Type(popup.PopupVisibleLocator + " input:text", newName);
                popup.OkWaitClosed();
            }

            Selenium.WaitElementPresent("{0}:contains('{1}')".Formato(headerSelector, newName));
        }


        public void MoveLeft(int columnIndex)
        {
            MoveColumn(columnIndex, left: true);
        }

        public void MoveRight(int columnIndex)
        {
            MoveColumn(columnIndex, left: false);
        }
        
        void MoveColumn(int columnIndex, bool left)
        {
            string headerSelector = HeaderCellLocator(columnIndex);

            string columnName = Selenium.GetEval("window.$('{0} input[type=hidden]').val()".Formato(headerSelector.RemoveStart(3)));
            
            string targetSelector = left ?
                "{0} .sf-header-droppable-left".Formato(HeaderCellLocator(columnIndex - 1)) :
                "{0} .sf-header-droppable-right".Formato(HeaderCellLocator(columnIndex + 1));

            Selenium.DragAndDropToObject(headerSelector, targetSelector);

            Selenium.WaitElementPresent("{0}:has(input[type=hidden][value='{1}'])".Formato(
                HeaderCellLocator(left ? (columnIndex - 1) : (columnIndex + 1)),
                columnName));
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

        public string EntityContextMenuLocator
        {
            get 
            {
                if (IsContext)
                    return "{0} td:nth-child({1}) .sf-search-ctxmenu:visible".Formato(resultTable.RowsLocator, 1 + 1);
                else
                    return "jq=#{0}sfTmSelected .sf-menu-button:visible".Formato(resultTable.PrefixUnderscore);
            }
        }

        public void QuickLinkClick(int quickLinkIndex)
        {
            resultTable.Selenium.Click("{0} .sf-search-ctxmenu-quicklinks .sf-search-ctxitem a:nth-child({1})".Formato(EntityContextMenuLocator, quickLinkIndex + 1));
        }

        public SearchPopupProxy QuickLinkClickSearch(int quickLinkIndex)
        {
            QuickLinkClick(quickLinkIndex);
            var result = new SearchPopupProxy(resultTable.Selenium, resultTable.PrefixUnderscore + "New");
            resultTable.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            resultTable.Selenium.WaitSearchCompleted();
            return result;
        }

        public void ExecuteClick(Enum operationKey)
        {
            MenuClick(operationKey.GetType().Name + "_" + operationKey.ToString());
            resultTable.Selenium.WaitElementDisapear(EntityContextMenuLocator); 
        }

        public void DeleteClick(Enum operationKey)
        {
            MenuClick(operationKey.GetType().Name + "_" + operationKey.ToString());
            resultTable.Selenium.ConsumeConfirmation();
        }

        public PopupControl<ProcessDN> DeleteProcessClick(Enum operationKey)
        {
            MenuClick(operationKey.GetType().Name + "_" + operationKey.ToString());
            resultTable.Selenium.ConsumeConfirmation();

            var result = new PopupControl<ProcessDN>(this.resultTable.Selenium, "New");
            result.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            return result;
        }


        public void MenuClick(string itemId)
        {
            resultTable.Selenium.Click(MenuItemLocator(itemId));
        }

        public string MenuItemLocator(string itemId)
        {
            return "{0} li.sf-search-ctxitem a#{1}".Formato(EntityContextMenuLocator, itemId);
        }

        public bool IsDisabled(string itemId)
        {
            return resultTable.Selenium.IsElementPresent(MenuItemLocator(itemId) + ".sf-disabled");
        }

        public bool IsDisabled(Enum operationKey)
        {
            return IsDisabled(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public PopupControl<T> ConstructFromPopup<T>(Enum operationKey) where T : IdentifiableEntity
        {
            MenuClick(operationKey.GetType().Name + "_" + operationKey.ToString());
            resultTable.Selenium.WaitElementDisapear(EntityContextMenuLocator);
            var result = new PopupControl<T>(this.resultTable.Selenium, "New");
            result.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            return result;
        }

        public NormalPage<T> ConstructFromNormalPage<T>(Enum operationKey) where T : IdentifiableEntity
        {
            MenuClick(operationKey.GetType().Name + "_" + operationKey.ToString());
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
            var combo = ElementsPerPageLocator;
            SearchControl.Selenium.Select(combo, "value=" + elementPerPage.ToString());
            SearchControl.Selenium.FireEvent(combo, "change");
            SearchControl.WaitSearchCompleted();
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
