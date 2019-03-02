using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.React.Selenium
{
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

        public WebElementLocator RowsLocator
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

        public List<Lite<IEntity>> SelectedEntities()
        {
            return RowsLocator.FindElements()
                .Where(tr => tr.IsElementPresent(By.CssSelector("input.sf-td-selection:checked")))
                .Select(a => Lite.Parse<IEntity>(a.GetAttribute("data-entity")))
                .ToList();
        }

        public WebElementLocator CellElement(int rowIndex, string token)
        {
            var index = GetColumnIndex(token);

            return RowElement(rowIndex).CombineCss("> td:nth-child({0})".FormatWith(index + 1));
        }

        public WebElementLocator CellElement(Lite<IEntity> lite, string token)
        {
            var index = GetColumnIndex(token);

            return RowElement(lite).CombineCss("> td:nth-child({0})".FormatWith(index + 1));
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
            return this.Element.WithLocator(By.CssSelector("tr[data-row-index='{0}'] .sf-td-selection".FormatWith(rowIndex)));
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
            return RowElement(lite).CombineCss(" > td:nth-child({0}) > a".FormatWith(entityIndex + 1));
        }

        public WebElementLocator EntityLink(int rowIndex)
        {
            var entityIndex = GetColumnIndex("Entity");
            return RowElement(rowIndex).CombineCss(" > td:nth-child({0}) > a".FormatWith(entityIndex + 1));
        }


        public FrameModalProxy<T> EntityClick<T>(Lite<T> lite) where T : Entity
        {
            var element = EntityLink(lite).Find().CaptureOnClick();
            return new FrameModalProxy<T>(element);
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
            return this.Element.FindElements(By.CssSelector("tbody > tr[data-entity]")).Count;
        }

        public Lite<Entity> EntityInIndex(int index)
        {
            var result = this.Element.FindElement(By.CssSelector("tbody > tr:nth-child(" + (index + 1) + ")")).GetAttribute("data-entity");

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
            lites.ForEach(lite => RowElement(lite).CombineCss(".sf-entity-ctxmenu-success").WaitVisible());
        }

        public void WaitNoVisible(Lite<IEntity> lite) => WaitNoVisible(new List<Lite<IEntity>> { lite });

        public void WaitNoVisible(List<Lite<IEntity>> lites)
        {
            lites.ForEach(lite => RowElement(lite).WaitNoVisible());
        }
    }
}
