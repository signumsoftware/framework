using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public class SearchPopupProxy : Popup
    {
        public SearchControlProxy SearchControl { get; private set; }

        public SearchPopupProxy(ISelenium selenium, string prefix) : base(selenium, prefix)
        {
            this.SearchControl = new SearchControlProxy(selenium, prefix);
        }
    }

    public class SearchPageProxy
    {
        public SearchControlProxy SearchControl { get; private set; }

        public SearchPageProxy(ISelenium selenium)
        {
            this.SearchControl = new SearchControlProxy(selenium, "");
        }
    }

    public class SearchControlProxy
    {
        public ISelenium Selenium { get; private set; }

        public string Prefix { get; private set; }

        public PaginationSelectorProxy Pagination { get; private set; }
        public ResultTableProxy Results { get; private set; }

        public SearchControlProxy(ISelenium selenium, string prefix)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
            this.Pagination = new PaginationSelectorProxy(selenium, prefix);
            this.Results = new ResultTableProxy(selenium, prefix); 
        }

        public string SearchButtonLocator
        {
            get { return "jq=#{0}qbSearch".Formato(Prefix); }
        }

        public void Search()
        {
            var searchButton = SearchButtonLocator;
            Selenium.Click(searchButton);

            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent(searchButton) &&
             !Selenium.IsElementPresent("{0}.sf-searching".Formato(searchButton)));
        }


        public string ToggleFiltersLocator
        {
            get { return "jq=#{0}sfSearchControl .sf-filters-header".Formato(Prefix); }
        }

        public string FiltersPanelLocator
        {
            get { return "jq=#{0}sfSearchControl .sf-filters".Formato(Prefix); }
        }

        public void ToggleFilters(bool show)
        {
            Selenium.Click(ToggleFiltersLocator);
            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent(FiltersPanelLocator + (show ? ":visible" : ":hidden")));
        }

        public string TokenLocator(int tokenIndex)
        {
            return "{0}ddlTokens_{1}".Formato(Prefix, tokenIndex);
        }

        public void WaitTokenCharged(int tokenIndex)
        {
            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent("{0}ddlTokens_{1}".Formato(Prefix, tokenIndex)));
        }

        public string AddFilterButtonLocator
        {
            get { return "jq=#{0}btnAddFilter".Formato(Prefix); }
        }

        public bool IsAddFilterEnabled
        {
            get
            {
                Selenium.AssertElementPresent(AddFilterButtonLocator);

                return Selenium.IsElementPresent(AddFilterButtonLocator + ":not(.ui-button-disabled)");
            }
        }

        public string AddColumnButtonLocator
        {
            get { return "jq=#{0}btnAddColumn".Formato(Prefix); }
        }

        public bool IsAddColumnEnabled
        {
            get
            {
                Selenium.AssertElementPresent(AddColumnButtonLocator);

                return Selenium.IsElementPresent(AddColumnButtonLocator + ":not(.ui-button-disabled)");
            }
        }


        public FilterOptionProxy AddFilter(int filterIndex)
        {
            Selenium.Click(AddFilterButtonLocator);

            FilterOptionProxy filter = new FilterOptionProxy(Selenium, Prefix, filterIndex);
            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent(filter.OperationLocator));
            return filter;
        }

        public FilterOptionProxy AddQuickFilter(int rowIndex, int columnIndex, int filterIndex)
        {
            string cellLocator = Results.CellLocator(rowIndex, columnIndex);
            Selenium.ContextMenu(cellLocator);

            string quickFilterLocator = "{0} .sf-quickfilter > span".Formato(cellLocator);
            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent(quickFilterLocator));
            Selenium.Click(quickFilterLocator);

            FilterOptionProxy filter = new FilterOptionProxy(Selenium, Prefix, filterIndex);
            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent(filter.OperationLocator));
            return filter;
        }

        public FilterOptionProxy AddQuickFilter(int columnIndex, int filterIndex)
        {
            string cellLocator = Results.HeaderCellLocator(columnIndex);
            Selenium.ContextMenu(cellLocator);

            string quickFilterLocator = "{0} .sf-quickfilter-header > span".Formato(cellLocator);
            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent(quickFilterLocator));
            Selenium.Click(quickFilterLocator);

            FilterOptionProxy filter = new FilterOptionProxy(Selenium, Prefix, filterIndex);
            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent(filter.OperationLocator));
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
            get { return QueryButtonLocator(Prefix + "qbSearchCreate"); }
        }

        public void Create()
        {
            Selenium.Click(CreateButtonLocator);
        }

        public void Create(Type typeToChoose)
        {
            Selenium.Click(CreateButtonLocator);

            //implementation popup opens
            Selenium.WaitAjaxFinished(() => Popup.IsPopupVisible(Selenium, Prefix));

            if (Popup.IsChooser(Selenium, Prefix))
                throw new InvalidOperationException("{0} is not a Chooser".Formato(Selenium)); 

            Selenium.Click(TypeLogic.GetCleanName(typeToChoose));
            Selenium.WaitAjaxFinished(() => !Popup.IsChooser(Selenium, Prefix));
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

    public class ResultTableProxy
    {  
        public ISelenium Selenium { get; private set; }

        public string Prefix { get; private set; }

        public ResultTableProxy(ISelenium selenium, string prefix)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
        }

        public string RowsLocator
        {
            get { return "jq=#{0}tblResults > tbody > tr".Formato(Prefix); }
        }

        public string RowLocator(int rowIndex)
        {
            int index = rowIndex + (HasMultiplyMessage ? 1 : 0);

            return RowsLocator + ":nth-child({0})".Formato(index + 1); 
        }

        public string RowLocator(Lite<IdentifiableEntity> lite)
        {
            return "{0}[data-entity='{1}']".Formato(RowsLocator, lite.Key());
        }

        public string CellLocator(int rowIndex, int columnIndex)
        {
            return RowLocator(rowIndex) + "> td:nth-child({0})".Formato(columnIndex + 1);
        }

        public string RowSelectorLocator(int rowIndex)
        {
            return "{0}rowSelection_{1}".Formato(Prefix, rowIndex);
        }

        public void SelectRow(int rowIndex)
        {
            Selenium.Click(RowSelectorLocator(rowIndex));
        }

        public void SelectRow(Lite<IdentifiableEntity> lite)
        {
            Selenium.Click(RowLocator(lite) + " .sf-td-selection");
        }

        public bool HasMultiplyMessage
        {
            get { return Selenium.IsElementPresent("jq=#{0}tblResults > tbody > tr.sf-tr-multiply".Formato(Prefix)); }
        }

        public string HeaderLocator
        {
            get { return "jq=#{0}tblResults > thead > tr > th".Formato(Prefix);  }
        }

        public string HeaderCellLocator(int columnIndex)
        {
            return HeaderLocator + ":nth-child({0})".Formato(columnIndex + 1);
        }

        public bool HasColumn(string queryToken)
        {
            return Selenium.IsElementPresent("{0} > :hidden[value='{1}']".Formato(HeaderLocator, queryToken));
        }

        public void RemoveColumn(int columnIndex, int numberOfColumnsBeforeDeleting)
        {
            string lastHeaderSelector = HeaderCellLocator(numberOfColumnsBeforeDeleting);
            string headerSelector = HeaderCellLocator(columnIndex);
            Selenium.ContextMenu(headerSelector);
            Selenium.Click("{0} .sf-remove-column > span".Formato(headerSelector));
            Selenium.WaitAjaxFinished(() => !Selenium.IsElementPresent(lastHeaderSelector));
        }

        public string EntityLinkLocator(Lite<IdentifiableEntity> lite, bool allowMultiple = false)
        {
            return RowLocator(lite) + " > td:nth-child({0}) > a".Formato(allowMultiple ? 2 : 1);
        }

        public string EntityLinkLocator(int rowIndex, bool allowMultiple = false)
        {
            return RowLocator(rowIndex) + " > td:nth-child({0}) > a".Formato(allowMultiple ? 2 : 1);
        }

        public void EntityClick(Lite<IdentifiableEntity> lite, bool allowMultiple = false)
        {
            Selenium.Click(EntityLinkLocator(lite, allowMultiple)); 
        }

        public void EntityClick(int rowIndex, bool allowMultiple = false)
        {
            Selenium.Click(EntityLinkLocator(rowIndex, allowMultiple));
        }

        public string EntityContextMenuLocator
        {
            get { return "{0} td:nth-child({1}) .sf-search-ctxmenu:visible".Formato(RowsLocator, 1); }
        }

        public void EntityContextMenu(int rowIndex)
        {
            Selenium.ContextMenu(CellLocator(rowIndex, 1));
            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent(EntityContextMenuLocator));
        }

        public void EntityContextQuickLinkClick(int quickLinkIndex)
        {
            Selenium.Click("{0} .sf-search-ctxmenu-quicklinks .sf-search-ctxitem a:nth-child({1})".Formato(quickLinkIndex));
        }

        public bool ThereAreNRows(int n)
        {
            string footerRow = RowLocator(n);
            string noRow = RowLocator(n + 1);

            return Selenium.IsElementPresent(footerRow) && !Selenium.IsElementPresent(noRow);
        }
        
    }

    public class PaginationSelectorProxy
    {
        public ISelenium Selenium { get; private set; }

        public string Prefix { get; private set; }

        public PaginationSelectorProxy(ISelenium selenium, string prefix)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
        }

        public string ElementsPerPageLocator
        {
            get { return "jq=#{0}sfElems".Formato(Prefix); }
        }

        public void SetElementsPerPage(int elementPerPage)
        {
            var combo = ElementsPerPageLocator;
            Selenium.Select(combo, "value=" + elementPerPage.ToString());
            Selenium.FireEvent(combo, "change");
        }

        public string PaginationModeLocator
        {
            get { return "jq=#{0}sfPaginationMode".Formato(Prefix); }
        }

        public void SetPaginationMode(PaginationMode mode)
        {
            var combo = PaginationModeLocator;
            Selenium.Select(combo, "value=" + mode.ToString());
            Selenium.FireEvent(combo, "change");
        }
    }

    public class FilterOptionProxy
    {
        public ISelenium Selenium { get; private set; }
        public string Prefix { get; private set; }
        public int FilterIndex { get; private set; }

        public FilterOptionProxy(ISelenium selenium, string prefix, int index)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
            this.FilterIndex = index;
        }

        public string OperationLocator
        {
            get { return "jq=#{0}ddlSelector_{1}".Formato(Prefix, FilterIndex); }
        }

        public string DeleteButtonLocator
        {
            get { return "jq=#{0}btnDelete_{1}".Formato(Prefix, FilterIndex); }
        }

        public void Delete()
        {
            Selenium.Click(DeleteButtonLocator);
        }
    }
}
