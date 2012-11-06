using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;

namespace Signum.Web.Selenium
{
    public static class SearchTestExtensions
    {
        public static string SearchSelector(string prefix)
        {
            return "jq=#{0}qbSearch".Formato(prefix);
        }

        public static void Search(this ISelenium selenium)
        {
            Search(selenium, "");
        }

        public static void Search(this ISelenium selenium, string prefix)
        {
            string searchButton = SearchSelector(prefix);
            selenium.Click(searchButton);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(searchButton) && 
                !selenium.IsElementPresent("{0}.sf-searching".Formato(searchButton)));
        }

        public static void SetElementsPerPageToFinder(this ISelenium selenium, string elementsPerPage)
        {
            SetElementsPerPageToFinder(selenium, elementsPerPage, "");
        }

        public static void SetElementsPerPageToFinder(this ISelenium selenium, string elementsPerPage, string prefix)
        {
            selenium.Select("{0}sfElems".Formato(prefix), "value=" + elementsPerPage);
        }

        public static void ToggleFilters(this ISelenium selenium, bool show)
        {
            ToggleFilters(selenium, show, "");
        }

        public static void ToggleFilters(this ISelenium selenium, bool show, string prefix)
        {
            selenium.Click("jq=#{0}sfSearchControl .sf-filters-header".Formato(prefix));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#{0}sfSearchControl .sf-filters:{1}".Formato(prefix, show ? "visible" : "hidden")));
        }

        public static void FilterSelectToken(this ISelenium selenium, int tokenSelectorIndexBase0, string itemSelector, bool willExpand)
        {
            FilterSelectToken(selenium, tokenSelectorIndexBase0, itemSelector, willExpand, "");
        }

        public static void FilterSelectToken(this ISelenium selenium, int tokenSelectorIndexBase0, string itemSelector, bool willExpand, string prefix)
        {
            selenium.Select("{0}ddlTokens_{1}".Formato(prefix, tokenSelectorIndexBase0), itemSelector);
            if (willExpand)
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}ddlTokens_{1}".Formato(prefix, tokenSelectorIndexBase0 + 1)));
        }

        public static string FilterOperationSelector(int filterIndexBase0)
        {
            return FilterOperationSelector(filterIndexBase0, "");
        }

        public static string FilterOperationSelector(int filterIndexBase0, string prefix)
        {
            return "jq=#{0}ddlSelector_{1}".Formato(prefix, filterIndexBase0);
        }

        public static void CheckAddFilterEnabled(this ISelenium selenium, bool isEnabled)
        {
            CheckAddFilterEnabled(selenium, isEnabled, "");
        }

        public static void CheckAddFilterEnabled(this ISelenium selenium, bool isEnabled, string prefix)
        {
            bool enabled = selenium.IsElementPresent("jq=#{0}btnAddFilter:not(.ui-button-disabled)".Formato(prefix));
            if (isEnabled)
                Assert.IsTrue(enabled);
            else
                Assert.IsFalse(enabled);
        }        

        public static void AddFilter(this ISelenium selenium, int filterIndexBase0)
        {
            AddFilter(selenium, filterIndexBase0, "");
        }

        public static void AddFilter(this ISelenium selenium, int filterIndexBase0, string prefix)
        {
            selenium.Click("{0}btnAddFilter".Formato(prefix));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(FilterOperationSelector(filterIndexBase0, prefix)));
        }        

        public static void FilterSelectOperation(this ISelenium selenium, int filterIndexBase0, string optionSelector)
        {
            FilterSelectOperation(selenium, filterIndexBase0, optionSelector, "");
        }

        public static void FilterSelectOperation(this ISelenium selenium, int filterIndexBase0, string optionSelector, string prefix)
        {
            selenium.Select(FilterOperationSelector(filterIndexBase0, prefix), optionSelector);
        }

        public static void DeleteFilter(this ISelenium selenium, int filterIndexBase0)
        {
            DeleteFilter(selenium, filterIndexBase0, "");
        }

        public static void DeleteFilter(this ISelenium selenium, int filterIndexBase0, string prefix)
        {
            selenium.Click("jq=#{0}btnDelete_{1}".Formato(prefix, filterIndexBase0));
        }

        public static void QuickFilter(this ISelenium selenium, int rowIndexBase1, int columnIndexBase1, int filterIndexBase0)
        {
            QuickFilter(selenium, rowIndexBase1, columnIndexBase1, filterIndexBase0, "");
        }

        public static void QuickFilter(this ISelenium selenium, int rowIndexBase1, int columnIndexBase1, int filterIndexBase0, string prefix)
        {
            string cellSelector = SearchTestExtensions.CellSelector(selenium, rowIndexBase1, columnIndexBase1, prefix);
            selenium.ContextMenu(cellSelector);
            
            string quickFilterSelector = "{0} .quickfilter".Formato(cellSelector);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(quickFilterSelector));
            selenium.Click(quickFilterSelector);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#{0}tblFilters #{0}trFilter_{1}".Formato(prefix, filterIndexBase0)));
        }

        public static void QuickFilterFromHeader(this ISelenium selenium, int columnIndexBase1, int filterIndexBase0)
        {
            QuickFilterFromHeader(selenium, columnIndexBase1, filterIndexBase0, "");
        }

        public static void QuickFilterFromHeader(this ISelenium selenium, int columnIndexBase1, int filterIndexBase0, string prefix)
        {
            string headerSelector = SearchTestExtensions.TableHeaderSelector(columnIndexBase1, prefix);
            selenium.ContextMenu(headerSelector);
            selenium.Click("{0} .quickfilter-header".Formato(headerSelector));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#{0}tblFilters #{0}trFilter_{1}".Formato(prefix, filterIndexBase0)));
        }

        public static void RemoveColumn(this ISelenium selenium, int columnIndexBase1, int numberOfColumnsBeforeDeleting)
        {
            RemoveColumn(selenium, columnIndexBase1, numberOfColumnsBeforeDeleting, "");
        }

        public static void RemoveColumn(this ISelenium selenium, int columnIndexBase1, int numberOfColumnsBeforeDeleting, string prefix)
        {
            string lastHeaderSelector = SearchTestExtensions.TableHeaderSelector(numberOfColumnsBeforeDeleting, prefix);
            Assert.IsTrue(selenium.IsElementPresent(lastHeaderSelector));
            string headerSelector = SearchTestExtensions.TableHeaderSelector(columnIndexBase1, prefix);
            selenium.ContextMenu(headerSelector);
            selenium.Click("{0} .remove-column".Formato(headerSelector));
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent(lastHeaderSelector));
        }

        public static void EditColumnName(this ISelenium selenium, int columnIndexBase1, string newName)
        {
            EditColumnName(selenium, columnIndexBase1, newName, "");
        }

        public static void EditColumnName(this ISelenium selenium, int columnIndexBase1, string newName, string prefix)
        {
            string headerSelector = SearchTestExtensions.TableHeaderSelector(columnIndexBase1, prefix);
            selenium.ContextMenu(headerSelector);
            selenium.Click("{0} .edit-column".Formato(headerSelector));

            string popupPrefix = prefix + "newName_";
            string popupSelector = SeleniumExtensions.PopupSelector(popupPrefix);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(popupSelector));
            selenium.Type("{0} input:text".Formato(popupSelector), newName);

            selenium.PopupOk(popupPrefix);
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent(SeleniumExtensions.PopupSelector(popupPrefix)));
            Assert.IsTrue(selenium.IsElementPresent("{0}:contains('{1}')".Formato(headerSelector, newName)));
        }

        public static void MoveColumn(this ISelenium selenium, int columnIndexBase1, string columnName, bool left)
        {
            MoveColumn(selenium, columnIndexBase1, columnName, left, "");
        }

        public static void MoveColumn(this ISelenium selenium, int columnIndexBase1, string columnName, bool left, string prefix)
        {
            string headerSelector = SearchTestExtensions.TableHeaderSelector(columnIndexBase1, prefix);
            string targetSelector = left ? 
                "{0} .sf-header-droppable-left".Formato(SearchTestExtensions.TableHeaderSelector(columnIndexBase1 - 1, prefix)) :
                "{0} .sf-header-droppable-right".Formato(SearchTestExtensions.TableHeaderSelector(columnIndexBase1 + 1, prefix));
            
            selenium.DragAndDropToObject(headerSelector, targetSelector);

            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:contains('{1}')".Formato(
                SearchTestExtensions.TableHeaderSelector((left ? (columnIndexBase1 - 1) : (columnIndexBase1 + 1)), prefix),
                columnName)));
        }

        public static string RowSelector()
        {
            return RowSelector("");
        }

        public static string RowSelector(string prefix)
        {
            return "jq=#{0}tblResults > tbody > tr".Formato(prefix);
        }

        public static string RowSelector(ISelenium selenium, int rowIndexBase1)
        {
            return RowSelector(selenium, rowIndexBase1, "");
        }

        public static string RowSelector(ISelenium selenium, int rowIndexBase1, string prefix)
        {
            if (selenium.HasMultiplyMessage(true, prefix))
                rowIndexBase1 += 1;
            return "{0}:nth-child({1})".Formato(RowSelector(prefix), rowIndexBase1);
            //return "{0}:not(.sf-tr-multiply):eq({1})".Formato(RowSelector(prefix), rowIndexBase1 - 1);
        }

        public static string CellSelector(ISelenium selenium, int rowIndexBase1, int columnIndexBase1)
        {
            return CellSelector(selenium, rowIndexBase1, columnIndexBase1, "");
        }

        public static string CellSelector(ISelenium selenium, int rowIndexBase1, int columnIndexBase1, string prefix)
        {
            return "{0} > td:nth-child({1})".Formato(RowSelector(selenium, rowIndexBase1, prefix), columnIndexBase1);
        }

        public static void SelectRowCheckbox(this ISelenium selenium, int rowIndexBase0)
        {
            SelectRowCheckbox(selenium, rowIndexBase0, "");
        }

        public static void SelectRowCheckbox(this ISelenium selenium, int rowIndexBase0, string prefix)
        {
            selenium.Click("{0}rowSelection_{1}".Formato(prefix, rowIndexBase0));
        }

        public static string TableHeaderSelector()
        {
            return TableHeaderSelector("");    
        }

        public static string TableHeaderSelector(string prefix)
        {
            return "jq=#{0}tblResults > thead > tr > th".Formato(prefix);
        }

        public static string TableHeaderSelector(int columnIndexBase1)
        {
            return TableHeaderSelector(columnIndexBase1, "");
        }

        public static string TableHeaderSelector(int columnIndexBase1, string prefix)
        {
            return "{0}:nth-child({1})".Formato(TableHeaderSelector(prefix), columnIndexBase1);
        }

        public static void TableHasColumn(this ISelenium selenium, string tokenName)
        {
            TableHasColumn(selenium, tokenName, "");
        }
        
        public static void TableHasColumn(this ISelenium selenium, string tokenName, string prefix)
        {
            Assert.IsTrue(selenium.IsElementPresent("{0} > :hidden[value={1}]".Formato(TableHeaderSelector(prefix), tokenName)));
        }

        public static void AssertMultiplyMessage(this ISelenium selenium, bool isPresent)
        {
            AssertMultiplyMessage(selenium, isPresent, "");
        }

        public static void AssertMultiplyMessage(this ISelenium selenium, bool isPresent, string prefix)
        {
            bool present = HasMultiplyMessage(selenium, isPresent, prefix);
            if (isPresent)
                Assert.IsTrue(present);
            else
                Assert.IsFalse(present);
        }

        public static bool HasMultiplyMessage(this ISelenium selenium, bool isPresent)
        {
            return HasMultiplyMessage(selenium, isPresent, "");
        }

        public static bool HasMultiplyMessage(this ISelenium selenium, bool isPresent, string prefix)
        {
            return selenium.IsElementPresent("jq=#{0}tblResults > tbody > tr.sf-tr-multiply".Formato(prefix));
        }

        public static void CheckAddColumnEnabled(this ISelenium selenium, bool isEnabled)
        {
            CheckAddColumnEnabled(selenium, isEnabled, "");
        }

        public static void CheckAddColumnEnabled(this ISelenium selenium, bool isEnabled, string prefix)
        {
            bool enabled = selenium.IsElementPresent("jq=#{0}btnAddColumn:not(.ui-button-disabled)".Formato(prefix));
            if (isEnabled)
                Assert.IsTrue(enabled);
            else
                Assert.IsFalse(enabled);
        }

        public static void AddColumn(this ISelenium selenium, string columnTokenName)
        {
            AddColumn(selenium, columnTokenName, "");
        }

        public static void AddColumn(this ISelenium selenium, string columnTokenName, string prefix)
        {
            selenium.Click("{0}btnAddColumn".Formato(prefix));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0} > :hidden[value={1}]".Formato(TableHeaderSelector(prefix), columnTokenName)));
        }

        public static void Sort(this ISelenium selenium, int columnIndexBase1, bool ascending)
        {
            Sort(selenium, columnIndexBase1, ascending, "");
        }

        public static void Sort(this ISelenium selenium, int columnIndexBase1, bool ascending, string prefix)
        {
            selenium.Click(TableHeaderSelector(columnIndexBase1, prefix));
            selenium.TableHeaderMarkedAsSorted(columnIndexBase1, ascending, true, prefix);
        }

        public static void SortMultiple(this ISelenium selenium, int columnIndexBase1, bool ascending)
        {
            SortMultiple(selenium, columnIndexBase1, ascending, "");
        }

        public static void SortMultiple(this ISelenium selenium, int columnIndexBase1, bool ascending, string prefix)
        {
            selenium.ShiftKeyDown();
            selenium.Click(TableHeaderSelector(columnIndexBase1, prefix));
            selenium.ShiftKeyUp();
            selenium.TableHeaderMarkedAsSorted(columnIndexBase1, ascending, true, prefix);
        }

        public static void TableHeaderMarkedAsSorted(this ISelenium selenium, int columnIndexBase1, bool ascending, bool marked)
        {
            TableHeaderMarkedAsSorted(selenium, columnIndexBase1, ascending, marked, "");
        }

        public static void TableHeaderMarkedAsSorted(this ISelenium selenium, int columnIndexBase1, bool ascending, bool marked, string prefix)
        {
            bool isMarked = selenium.IsElementPresent("{0}.{1}".Formato(
                TableHeaderSelector(columnIndexBase1, prefix),
                ascending ? ".sf-header-sort-down" : ".sf-header-sort-up"));

            if (marked)
                Assert.IsTrue(isMarked);
            else
                Assert.IsFalse(isMarked);
        }

        public static bool IsElementInCell(this ISelenium selenium, int rowIndexBase1, int columnIndexBase1, string selector)
        {
            return IsElementInCell(selenium, rowIndexBase1, columnIndexBase1, selector, "");
        }

        public static bool IsElementInCell(this ISelenium selenium, int rowIndexBase1, int columnIndexBase1, string selector, string prefix)
        {
            return selenium.IsElementPresent(CellSelector(selenium, rowIndexBase1, columnIndexBase1, prefix) + " " + selector);
        }

        public static bool IsEntityInRow(this ISelenium selenium, int rowIndexBase1, string liteKey)
        {
            return IsEntityInRow(selenium, rowIndexBase1, liteKey, "");
        }

        public static bool IsEntityInRow(this ISelenium selenium, int rowIndexBase1, string liteKey, string prefix)
        {
            return selenium.IsElementPresent("{0}[data-entity='{1}']".Formato(RowSelector(selenium, rowIndexBase1, prefix), liteKey));
        }

        public static string EntityRowSelector(string liteKey)
        {
            return EntityRowSelector(liteKey, "");
        }

        public static string EntityRowSelector(string liteKey, string prefix)
        {
            return "{0}[data-entity='{1}']".Formato(RowSelector(prefix), liteKey);
        }

        public static void EntityClick(this ISelenium selenium, string liteKey)
        {
            EntityClick(selenium, liteKey, "");
        }

        public static void EntityClick(this ISelenium selenium, string liteKey, string prefix)
        {
            selenium.Click("{0} > td:first > a".Formato(EntityRowSelector(liteKey, prefix)));
        }

        public static void EntityClick(this ISelenium selenium, int rowIndexBase1)
        {
            EntityClick(selenium, rowIndexBase1, "");
        }

        public static void EntityClick(this ISelenium selenium, int rowIndexBase1, string prefix)
        {
            selenium.EntityClick(rowIndexBase1, prefix, true);
        }

        public static void EntityClick(this ISelenium selenium, int rowIndexBase1, string prefix, bool allowMultiple)
        {
            if (allowMultiple)
                selenium.Click("{0} > a".Formato(CellSelector(selenium, rowIndexBase1, 2, prefix)));
            else
                selenium.Click("{0} > a".Formato(CellSelector(selenium, rowIndexBase1, 1, prefix)));
        }

        public static string EntityContextMenuSelector(ISelenium selenium, int rowIndexBase1)
        {
            return EntityContextMenuSelector(selenium, rowIndexBase1, "");
        }

        public static string EntityContextMenuSelector(ISelenium selenium, int rowIndexBase1, string prefix)
        {
            return "{0} td:nth-child({1}) .sf-search-ctxmenu:visible".Formato(RowSelector(prefix), 1);
        }

        public static void EntityContextMenu(this ISelenium selenium, int rowIndexBase1)
        {
            EntityContextMenu(selenium, rowIndexBase1, "");
        }

        public static void EntityContextMenu(this ISelenium selenium, int rowIndexBase1, string prefix)
        {
            selenium.ContextMenu(CellSelector(selenium, rowIndexBase1, 1, prefix));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(EntityContextMenuSelector(selenium, rowIndexBase1, prefix)));
        }

        public static void EntityContextMenuClick(this ISelenium selenium, int rowIndexBase1, string itemId)
        {
            EntityContextMenuClick(selenium, rowIndexBase1, itemId, "");
        }

        public static void EntityContextMenuClick(this ISelenium selenium, int rowIndexBase1, string itemId, string prefix)
        {
            selenium.Click("{0} li.sf-search-ctxitem a#{1}".Formato(EntityContextMenuSelector(selenium, rowIndexBase1, prefix), itemId));
        }

        public static void EntityContextQuickLinkClick(this ISelenium selenium, int rowIndexBase1, int quickLinkIndexBase1)
        {
            EntityContextQuickLinkClick(selenium, rowIndexBase1, quickLinkIndexBase1, "");
        }

        public static void EntityContextQuickLinkClick(this ISelenium selenium, int rowIndexBase1, int quickLinkIndexBase1, string prefix)
        {
            selenium.Click("{0} .sf-search-ctxmenu-quicklinks .sf-search-ctxitem a:nth-child({1})".Formato(
                EntityContextMenuSelector(selenium, rowIndexBase1, prefix), 
                quickLinkIndexBase1));
        }

        public static Expression<Func<bool>> ThereAreNRows(this ISelenium selenium, int n, string prefix)
        {
            if (n == 0)
                n = 1; //there will be a row with the "no results" message

            string footerRow = RowSelector(selenium, n + 1, prefix);
            string noRow = RowSelector(selenium, n + 2, prefix);

            return () => selenium.IsElementPresent(footerRow) &&
                !selenium.IsElementPresent(noRow);
        }

        public static Expression<Func<bool>> ThereAreNRows(this ISelenium selenium, int n)
        {
            return ThereAreNRows(selenium, n, "");
        }

        public static string SearchCreateLocator()
        {
            return SearchCreateLocator("");
        }

        public static string SearchCreateLocator(string prefix)
        {
            return QueryButtonLocator(prefix + "qbSearchCreate");
        }

        public static void SearchCreate(this ISelenium selenium)
        {
            selenium.Click(SearchCreateLocator(""));
        }

        public static void SearchCreate(this ISelenium selenium, string prefix)
        {
            selenium.Click(SearchCreateLocator(prefix));
        }

        public static void SearchCreateWithImpl(this ISelenium selenium, string typeToChoose)
        {
            SearchCreateWithImpl(selenium, typeToChoose, "");
        }

        public static void SearchCreateWithImpl(this ISelenium selenium, string typeToChoose, string prefix)
        {
            selenium.Click(SearchCreateLocator(prefix));

            //implementation popup opens
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(prefix)));
            selenium.Click(typeToChoose);
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0} .sf-chooser-button".Formato(SeleniumExtensions.PopupSelector(prefix))));
        }

        public static string QueryButtonLocator(string id) {
            //check query-button class present is redundant for locating, but it must be there in the html so good for testing
            return "jq=#{0}.sf-query-button".Formato(id); 
        }

        public static string QueryMenuOptionLocator(string menuId, string optionId)
        {
            //check of menu and item classes is redundant but it must be in the html, so good for testing
            return "jq=#{0}.sf-dropdown ul.sf-menu-button li.ui-menu-item a.sf-query-button#{1}".Formato(menuId, optionId); 
        }

        public static string QueryMenuOptionLocatorByAttr(string menuId, string optionLocator)
        {
            //check of menu and item classes is redundant but it must be in the html, so good for testing
            return "jq=#{0}.sf-dropdown ul.sf-menu-button li.ui-menu-item a.sf-query-button[{1}]".Formato(menuId, optionLocator);
        }

        public static void QueryButtonClick(this ISelenium selenium, string id)
        {
            selenium.Click(QueryButtonLocator(id));
        }

        public static void QueryMenuOptionClick(this ISelenium selenium, string menuId, string optionId)
        {
            selenium.Click(QueryMenuOptionLocator(menuId, optionId));
        }

        public static void QueryMenuOptionPresent(this ISelenium selenium, string menuId, string optionId, bool present)
        {
            bool isPresent = selenium.IsElementPresent(QueryMenuOptionLocator(menuId, optionId));
            if (present)
                Assert.IsTrue(isPresent);
            else
                Assert.IsFalse(isPresent);
        }

        public static void QueryMenuOptionPresentByAttr(this ISelenium selenium, string menuId, string optionLocator, bool present)
        {
            bool isPresent = selenium.IsElementPresent(QueryMenuOptionLocatorByAttr(menuId, optionLocator));
            if (present)
                Assert.IsTrue(isPresent);
            else
                Assert.IsFalse(isPresent);
        }
    }
}
