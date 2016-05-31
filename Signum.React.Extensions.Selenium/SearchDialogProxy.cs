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

        public SearchPopupProxy(RemoteWebDriver selenium, IWebElement element)
            : base(selenium, element)
        {
            this.SearchControl = new SearchControlProxy(selenium, element);
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
            this.SearchControl = new SearchControlProxy(selenium, selenium.NotImplemented());
        }

        public PopupControl<T> Create<T>() where T : ModifiableEntity
        {
            SearchControl.CreateButton.Click();

            return new PopupControl<T>(Selenium, Selenium.NotImplemented("")).WaitVisible();
        }
        
        public PopupControl<T> CreateChoose<T>() where T : ModifiableEntity
        {
            SearchControl.CreateButton.Click();

            //implementation popup opens
            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, SearchControl.Element));

            if (!ChooserPopup.IsChooser(Selenium, SearchControl.Element))
                throw new InvalidOperationException("{0} is not a Chooser".FormatWith(Selenium));

            ChooserPopup.ChooseButton(Selenium, SearchControl.Element, typeof(T));

            return new PopupControl<T>(Selenium, Selenium.NotImplemented("Temp")).WaitVisible();
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

        public FiltersProxy Filters { get; private set; }
        public PaginationSelectorProxy Pagination { get; private set; }
        public ResultTableProxy Results { get; private set; }


        public SearchControlProxy(RemoteWebDriver selenium, IWebElement element)
        {
            this.Selenium = selenium;
            this.Element = element;
            this.Pagination = new PaginationSelectorProxy(this);
            this.Results = new ResultTableProxy(this.Selenium, this.Element, this.WaitSearchCompleted, hasDataEntity: true);
            this.Filters = new FiltersProxy(this.Selenium, this.Element);
        }

        public WebElementLocator SearchButton
        {
            get { return this.Element.WithLocator(By.ClassName("sf-search-control")); }
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

        public WebElementLocator ToggleFiltersButton
        {
            get { return this.Element.WithLocator(By.ClassName("sf-filters-header")); }
        }

        public WebElementLocator FiltersPanel
        {
            get { return this.Element.WithLocator(By.ClassName("sf-filters")) }
        }

        public void ToggleFilters(bool show)
        {
            ToggleFiltersButton.Find().Click();
            if (show)
                FiltersPanel.WaitVisible();
            else
                FiltersPanel.WaitNoVisible();
        }

        //public IWebElement AddColumnButtonElement
        //{
        //    get { throw new NotImplementedException(); /* return By.CssSelector("#{0}btnAddColumn".FormatWith(PrefixUnderscore)); */ }
        //}

        //public bool IsAddColumnEnabled
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //        //Selenium.AssertElementPresent(AddColumnButtonLocator);

        //        //return Selenium.IsElementPresent(AddColumnButtonLocator.CombineCss(":not([disabled])"));
        //    }
        //}

        //public void AddColumn(string token)
        //{
        //    throw new NotImplementedException();
        //    //Filters.QueryTokenBuilder.SelectToken(token);
        //    //Selenium.Wait(() => IsAddColumnEnabled);
        //    //Selenium.FindElement(AddColumnButtonLocator).Click();
        //    //Selenium.WaitElementPresent(Results.HeaderCellLocator(token));
        //}


        public FilterOptionProxy AddQuickFilter(int rowIndex, string token)
        {
            var newFilterIndex = Filters.NewFilterIndex();

            Selenium.FindElement(Results.CellLocator(rowIndex, token)).ContextClick();

            IWebElement quickFilterElement =  By.CssSelector("#sfContextMenu .sf-quickfilter");
            Selenium.WaitElementPresent(quickFilterLocator);
            Selenium.FindElement(quickFilterLocator).Click();

            FilterOptionProxy filter = new FilterOptionProxy(this.Filters, newFilterIndex);
            Selenium.WaitElementPresent(filter.OperationLocator);
            return filter;
        }

        public FilterOptionProxy AddQuickFilter(string token)
        { 
            //var newFilterIndex = Filters.NewFilterIndex();

            //var headerLocator = Results.HeaderCellLocator(token);
            //Selenium.FindElement(headerLocator).ContextClick();

            //IWebElement quickFilterElement = By.CssSelector("#sfContextMenu .sf-quickfilter-header");
            //Selenium.WaitElementPresent(quickFilterLocator);
            //Selenium.FindElement(quickFilterLocator).Click();

            //FilterOptionProxy filter = new FilterOptionProxy(this.Filters, newFilterIndex);
            //Selenium.WaitElementPresent(filter.OperationLocator);
            //return filter;
        }


        public IWebElement QueryButtonElement(string id)
        {
            throw new NotImplementedException();

            //return By.CssSelector("#{0}.sf-query-button".FormatWith(id));
        }

        public void QueryButtonClick(string id)
        {
            throw new NotImplementedException();

            //Selenium.FindElement(QueryButtonLocator(id)).Click();
        }

        public PopupControl<T> Create<T>() where T : ModifiableEntity
        {
            throw new NotImplementedException();
           

            //var element = this.PrefixUnderscore + "Temp";

            //Selenium.FindElement(CreateButtonLocator).Click();

            //Selenium.Wait(() => Popup.IsPopupVisible(Selenium, element));

            //return new PopupControl<T>(Selenium, element);
        }

        public PopupControl<T> CreateChoose<T>() where T : ModifiableEntity
        {
            throw new NotImplementedException();

            //Selenium.FindElement(CreateButtonLocator).Click();

            ////implementation popup opens
            //Selenium.Wait(() => Popup.IsPopupVisible(Selenium, Prefix));

            //if (!ChooserPopup.IsChooser(Selenium, Prefix))
            //    throw new InvalidOperationException("{0} is not a Chooser".FormatWith(Selenium));

            //ChooserPopup.ChooseButton(Selenium, Prefix, typeof(T));

            //return new PopupControl<T>(Selenium, Prefix);
        }

        public IWebElement CreateButton
        {
            get { throw new NotImplementedException(); /* return QueryButtonLocator(PrefixUnderscore + "qbSearchCreate");*/ }
        }

        public bool HasMultiplyMessage
        {
            get { throw new NotImplementedException(); /*  return Selenium.IsElementPresent(By.CssSelector("#{0}sfSearchControl .sf-td-multiply".FormatWith(PrefixUnderscore))); */ }
        }

        public IWebElement MenuOptionElement(string optionId)
        {

            //return By.CssSelector("#{0}sfSearchControl a#{1}".FormatWith(PrefixUnderscore, optionId));
        }

        public IWebElement MenuOptionElementByAttr(string optionLocator)
        {
            

            //return By.CssSelector("#{0}sfSearchControl a[{1}]".FormatWith(PrefixUnderscore, optionLocator));
        }

        public bool FiltersVisible
        {
            get { return this.FiltersPanel.IsVisible(); }
        }

        public ILineContainer<T> SimpleFilterBuilder<T>() where T : ModifiableEntity
        {
            return new LineContainer<T>(this.Selenium, this.Prefix);
        }
    }

    public class FiltersProxy
    {
        public RemoteWebDriver Selenium { get; private set; }
        public IWebElement Element { get; private set; }
        public QueryTokenBuilderProxy QueryTokenBuilder { get; private set; }

        public FiltersProxy(RemoteWebDriver selenium, IWebElement Element)
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

        public IWebElement AddFilterButtonElement
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
        public IWebElement Element { get; private set; }

        public QueryTokenBuilderProxy(RemoteWebDriver selenium, IWebElement element)
        {
            this.Selenium = selenium;
            this.Element = element;
        }

        public IWebElement TokenElement(int tokenIndex, string previousToken, bool isEnd)
        {
            var result = By.CssSelector("#{0}{1}_{2}".FormatWith(Element, !isEnd ? "ddlTokens" : "ddlTokensEnd", tokenIndex));

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

        public IWebElement Element;

        public Action<Action> WaitSearchCompleted;

        public bool HasDataEntity;

        public ResultTableProxy(RemoteWebDriver selenium, IWebElement elementUndescore, Action<Action> waitSearchCompleted, bool hasDataEntity)
        {
            this.Selenium = selenium;
            this.PrefixUnderscore = prefixUndescore;
            this.WaitSearchCompleted = waitSearchCompleted;
            this.HasDataEntity = hasDataEntity;
        }

        public IWebElement ResultTableElement
        {
            get { return By.CssSelector("#{0}tblResults".FormatWith(PrefixUnderscore)); }
        }

        public IWebElement RowsElement
        {
            get { return By.CssSelector("#{0}tblResults > tbody > tr".FormatWith(PrefixUnderscore)); }
        }

        public IWebElement RowElement(int rowIndex)
        {
            return RowsLocator.CombineCss(":nth-child({0})".FormatWith(rowIndex + 1));
        }

        public IWebElement RowElement(Lite<IEntity> lite)
        {
            return RowsLocator.CombineCss("[data-entity='{0}']".FormatWith(lite.Key()));
        }

        public IWebElement CellElement(int rowIndex, string token)
        {
            var index = GetColumnIndex(token);

            return RowLocator(rowIndex).CombineCss("> td:nth-child({0})".FormatWith(index + 1));
        }

        public IWebElement CellElement(Lite<IEntity> lite, string token)
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

        public IWebElement RowSelectorElement(int rowIndex)
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

        public IWebElement HeaderElement
        {
            get { return By.CssSelector("#{0}tblResults > thead > tr > th".FormatWith(PrefixUnderscore)); }
        }

        public string[] GetColumnTokens()
        {
            var array = (string)Selenium.ExecuteScript("return $('#" + PrefixUnderscore + "tblResults > thead > tr > th')" +
                ".toArray().map(function(e){ return  $(e).hasClass('sf-th-entity')? 'Entity' : $(e).attr('data-column-name'); }).join(',')");

            return array.Split(',');
        }

        public IWebElement HeaderCellElement(string token)
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

        public IWebElement EntityLinkElement(Lite<IEntity> lite, bool allowSelection = true)
        {
            return RowLocator(lite).CombineCss(" > td:nth-child({0}) > a".FormatWith(allowSelection ? 2 : 1));
        }

        public IWebElement EntityLinkElement(int rowIndex, bool allowSelection = true)
        {
            return RowLocator(rowIndex).CombineCss(" > td:nth-child({0}) > a".FormatWith(allowSelection ? 2 : 1));
        }


        public IWebElement EntityLinkButton(Lite<IEntity> lite, bool allowSelection = true)
        {
            return Selenium.FindElement(EntityLinkLocator(lite, allowSelection));
        }

        public IWebElement EntityLinkButton(int rowIndex, bool allowSelection = true)
        {
            return Selenium.FindElement(EntityLinkLocator(rowIndex, allowSelection));
        }

        public PopupControl<T> EntityClick<T>(Lite<T> lite, bool allowSelection = true) where T : Entity
        {
            EntityLinkButton(lite, allowSelection).Click();
            return new PopupControl<T>(Selenium, this.PrefixUnderscore + "nav").WaitVisible();
        }

        public PopupControl<T> EntityClick<T>(int rowIndex, bool allowSelection = true) where T : Entity
        {
            EntityLinkButton(rowIndex, allowSelection).Click();
            return new PopupControl<T>(Selenium, this.PrefixUnderscore + "nav").WaitVisible();
        }

        public NormalPage<T> EntityClickNormalPage<T>(Lite<T> lite, bool allowSelection = true) where T : Entity
        {
            EntityLinkButton(lite, allowSelection).Click();
            return new NormalPage<T>(Selenium).WaitLoaded();
        }

        public NormalPage<T> EntityClickNormalPage<T>(int rowIndex, bool allowSelection = true) where T : Entity
        {
            EntityLinkButton(rowIndex, allowSelection).Click();
            return new NormalPage<T>(Selenium).WaitLoaded();
        }

        public EntityContextMenuProxy EntityContextMenu(int rowIndex, string columnToken = "Entity")
        {
            Selenium.FindElement(CellLocator(rowIndex, columnToken)).ContextClick();

            EntityContextMenuProxy ctx = new EntityContextMenuProxy(this, isContext: true);

            ctx.WaitNotLoading();

            return ctx;
        }

        public EntityContextMenuProxy EntityContextMenu(Lite<Entity> lite, string columnToken = "Entity")
        {
            Selenium.FindElement(CellLocator(lite, columnToken)).ContextClick();

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
            return Selenium.FindElement(CellElement(rowIndex, token)).FindElements(locator).Any();
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

        public void WaitActiveSuccess()
        {
            Selenium.WaitElementVisible(RowsLocator.CombineCss(".active.sf-entity-ctxmenu-success"));
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

        public IWebElement EntityContextMenuElement
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

            resultTable.WaitActiveSuccess();
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

            resultTable.WaitActiveSuccess();
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

        public IWebElement MenuItemElement(string itemId)
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

        public PopupControl<T> MenuClickPopup<T>(string itemId, IWebElement element = "New")
            where T : Entity
        {
            MenuClick(itemId);
            //resultTable.Selenium.WaitElementDisapear(EntityContextMenuLocator);
            var result = new PopupControl<T>(this.resultTable.Selenium, element);
            result.Selenium.WaitElementPresent(result.PopupLocator);
            return result;
        }

        public PopupControl<T> MenuClickPopup<T>(IOperationSymbolContainer contanier, IWebElement element = "New")
            where T : Entity
        {
            return MenuClickPopup<T>(contanier.Symbol.KeyWeb(), element);
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
               !this.resultTable.Selenium.FindElement(this.EntityContextMenuLocator)
                    .FindElements(By.CssSelector("li.sf-tm-selected-loading")).Any());
        }
    }

    public class PaginationSelectorProxy
    {
        public SearchControlProxy SearchControl { get; private set; }

        public PaginationSelectorProxy(SearchControlProxy seachControl)
        {
            this.SearchControl = seachControl;
        }

        public IWebElement ElementsPerPageElement
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

        public IWebElement PaginationModeElement
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

        public IWebElement OperationElement
        {
            get { return By.CssSelector("#{0}ddlSelector_{1}".FormatWith(Filters.PrefixUnderscore, FilterIndex)); }
        }

        public FilterOperation Operation
        {
            get { return Filters.Selenium.FindElement(OperationLocator).SelectElement().SelectedOption.GetAttribute("value").ToEnum<FilterOperation>(); }
            set { Filters.Selenium.FindElement(OperationLocator).SelectElement().SelectByValue(value.ToString()); }
        }

        public IWebElement DeleteButtonElement
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
