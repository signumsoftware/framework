using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine.Basics;
using System.Windows;
using Signum.Entities.Basics;
using System.Threading.Tasks;
using System.Threading;

namespace Signum.Windows.UIAutomation
{
    public class SearchWindowProxy : WindowProxy
    {
        public SearchControlProxy SearchControl { get; private set; }

        public SearchWindowProxy(AutomationElement element)
            : base(element.AssertClassName("SearchWindow"))
        {
            SearchControl = new SearchControlProxy(element.ChildById("searchControl"));
        }

        public FilterOptionProxy AddFilterString(string token, FilterOperation operation, string value)
        {
            return SearchControl.AddFilterString(token, operation, value);
        }

        public void AddColumn(string token, string displayName)
        {
            SearchControl.AddColumn(token, displayName);
        }

        public void SortColumn(string token, OrderType orderType)
        {
            SearchControl.SortColumn(token, orderType);
        }

        public void Search()
        {
            SearchControl.Search();
        }

        public SearchWindowProxy SearchSelectAt(int index)
        {
            Search();
            SelectElementAt(index);
            return this;
        }

        public void WaitSearch()
        {
            SearchControl.WaitSearch();
        }

        public NormalWindowProxy<T> Create<T>() where T : Entity
        {
            return SearchControl.Create<T>();
        }

        public NormalWindowProxy<T> ViewElementAt<T>(int index) where T : Entity
        {
            return SearchControl.ViewElementAt<T>(index);
        }

        public void SelectElementAt(int index)
        {
            SearchControl.SelectElementAt(index);
        }

        public AutomationElement OkButton
        {
            get { return Element.Child(c => c.Current.ClassName == "OkCancelBar").ChildById("btOk"); }
        }

        public AutomationElement CanelButton
        {
            get { return Element.Child(c => c.Current.ClassName == "OkCancelBar").ChildById("btCancel"); }
        }

        public void Ok()
        {
            OkButton.ButtonInvoke();
            Element.Wait(() => this.IsClosed,
                () => "Waiting for SearchWindow {0} to close after Ok pressed".FormatWith(Element.Current.Name));
        }

        public AutomationElement OkCapture(int? timeout = null)
        {
            return Element.CaptureWindow(
                () => OkButton.ButtonInvoke(),
                () => "Accept button on search window", timeout);
        }

        public static void Select(AutomationElement element, int index = 0)
        {
            using (var searchWindow = new SearchWindowProxy(element))
            {
                if (searchWindow.SearchControl.SearchButton.Current.IsEnabled == true)
                    searchWindow.Search();
                else
                    searchWindow.WaitSearch();

                searchWindow.SelectElementAt(index);
                searchWindow.Ok();
            }
        }

        public static AutomationElement SearchAndSelectWindow(AutomationElement element, int index = 0)
        {
            using (var searchWindow = new SearchWindowProxy(element))
            {
                if (searchWindow.SearchControl.SearchButton.Current.IsEnabled == true)
                    searchWindow.Search();
                else
                    searchWindow.WaitSearch();

                searchWindow.SelectElementAt(index);
                return searchWindow.OkCapture();
            }
        }
    }

    public class SearchControlProxy
    {
        public AutomationElement Element { get; private set; }

        public SearchControlProxy(AutomationElement element)
        {
            this.Element = element;
            this.HeaderMap = new ResetLazy<Dictionary<string, int>>(() =>
                HeaderItems
                .Select((e, i) => KVP.Create(e.Current.Name, i))
                .AgGroupToDictionary(a => a.Key, gr => gr.First().Value));
        }

        public void SelectToken(string token)
        {
            var tokens = token.Split('.');

            var tb = Element.ChildById("tokenBuilder");

            for (int i = 0; i < tokens.Length; i++)
            {
                List<AutomationElement> combos = null;

                tb.Wait(() =>
                {
                    combos = tb.Children(a => a.Current.ControlType == ControlType.ComboBox);
                    return combos.Count > i;
                }, () => "Finding combo for token " + token[i] + (i == 0 ? "" : " after token {0}".FormatWith(token[i - 1])));

                combos[i].ComboSelectItem(a => a.Current.Name == tokens[i]);
            }
        }

        public void AddFilter()
        {
            Element.ChildById("btCreateFilter").ButtonInvoke();
        }

        public FilterOptionProxy LastFilter()
        {
            TreeWalker tw = new TreeWalker(ConditionBuilder.ToCondition(ae => ae.Current.ClassName == "FilterLine"));

            var filterLine = tw.GetLastChild(FilterBuilderControl);

            if (filterLine == null)
                throw new ElementNotAvailableException("Last FilterLine not found");

            return new FilterOptionProxy(filterLine, this);
        }

        public AutomationElement FilterBuilderControl
        {
            get { return Element.ChildById("filterBuilder"); }
        }

        public AutomationElement SearchButton
        {
            get { return Element.ChildById("btSearch"); }
        }

        public List<AutomationElement> HeaderItems
        {
            get
            {
                return Element.Descendants(e => e.Current.ControlType == ControlType.HeaderItem);
            }

        }

        public void Search()
        {
            SearchButton.ButtonInvoke();
            WaitSearch();
        }

        public void WaitSearch()
        {
            Element.Wait(
                () =>
                {
                    Element.AssertMessageBoxChild();
                    return SearchButton.Current.IsEnabled;
                },
                () => "Waiting after search on SearchControl {0}".FormatWith(Element.Current.Name));
        }

        public AutomationElement FilterToggle
        {
            get
            {
                return Element.ChildById("btFilters");
            }
        }

        public FilterOptionProxy AddFilterString(string token, FilterOperation operation, string value)
        {
            FilterToggle.SetCheck(true);

            SelectToken(token);

            AddFilter();

            var lastFilter = LastFilter();

            lastFilter.SetOperation(operation);

            lastFilter.SetValueString(value);

            return lastFilter;
        }

        public void AddColumn(string token, string displayName)
        {
            SelectToken(token);

            var win = Element.CaptureChildWindow(() => Element.ChildById("btCreateColumn").ButtonInvoke(),
                () => "Adding new column for {0} on SearchControl {1}".FormatWith(token, Element.Current.Name));

            using (WindowProxy wp = new WindowProxy(win))
            {
                if (displayName != null)
                    wp.Element.Descendant(a => a.Current.ControlType == ControlType.Edit).Pattern<ValuePattern>().SetValue(displayName);

                wp.Element.ChildById("btAccept").ButtonInvoke();
            }
        }

        public NormalWindowProxy<T> Navigate<T>(int? timeOut = null) where T : Entity
        {
            if (NavigateButton.Current.IsOffscreen)
                throw new InvalidOperationException("Navigate button not visible on SearchControl {0}".FormatWith(Element.Current.Name));

            var win = Element.CaptureWindow(
                () => NavigateButton.ButtonInvoke(),
                () => "Navigate selected entity on SearchControl ({0})".FormatWith(Element.Current.Name), timeOut);

            return new NormalWindowProxy<T>(win);
        }

        public NormalWindowProxy<T> Create<T>() where T : Entity
        {
            return CreateCapture().ToNormalWindow<T>();
        }

        public AutomationElement CreateCapture(int? timeOut = null)
        {
            if (CreateButton.Current.IsOffscreen)
                throw new InvalidOperationException("Create button not visible on SearchControl {0}".FormatWith(Element.Current.Name));

            var win = Element.CaptureWindow(
                () => CreateButton.ButtonInvoke(),
                () => "Create a new entity on SearchControl ({0})".FormatWith(Element.Current.Name), timeOut);

            return win;
        }

        public AutomationElement CreateButton
        {
            get { return Element.ChildById("btCreate"); }
        }

        public AutomationElement NavigateButton
        {
            get { return Element.ChildById("btNavigate"); }
        }


        public AutomationElement Menu
        {
            get { return Element.ChildById("menu"); }
        }

        public AutomationElement Results
        {
            get { return Element.ChildById("lvResult"); }
        }

        public AutomationElement ResultColumns
        {
            get { return Results.Child(a => a.Current.ControlType == ControlType.Header); }
        }

        public void SortColumn(string token, OrderType orderType)
        {
            var columnHeader = ResultColumns.Child(a => a.Current.ControlType == ControlType.HeaderItem && a.Current.Name == token);

            columnHeader.ButtonInvoke();

            while (columnHeader.Current.ItemStatus != orderType.ToString())
                columnHeader.ButtonInvoke();

            WaitSearch();
        }

        public List<ListViewItemProxy> GetRows()
        {
            return Results.Children(c => c.Current.ControlType == ControlType.DataItem).Select(ae => new ListViewItemProxy(ae, this)).ToList();
        }

        public bool HasRows()
        {
            return Results.TryChild(c => c.Current.ControlType == ControlType.DataItem) != null;
        }

        public NormalWindowProxy<T> ViewElementAt<T>(int index) where T : Entity
        {
            SelectElementAt(index);

            return Navigate<T>();
        }

        ResetLazy<Dictionary<string, int>> HeaderMap;

        public void SelectElementAt(int index)
        {
            ElementAt(index).Select();
        }

        public class ListViewItemProxy
        {
            public ListViewItemProxy(AutomationElement element, SearchControlProxy sc)
            {
                this.Element = element;
                this.SearchControl = sc;
            }

            public SearchControlProxy SearchControl { get; private set; }

            public AutomationElement Element { get; private set; }

            List<AutomationElement> columns;
            public List<AutomationElement> Columns
            {
                get
                {
                    return columns ?? (columns = Element.Children(a => a.Current.ClassName == "ContentPresenter"));
                }
            }

            public AutomationElement Column(string tokenName)
            {
                return Columns[SearchControl.HeaderMap.Value.GetOrThrow(tokenName,
                    tb => new ElementNotFoundException("{0} not found on query {1}".FormatWith(tb, SearchControl.Element.Current.Name)))];
            }

            public void Select()
            {
                Element.Pattern<SelectionItemPattern>().Select();
            }

            public Lite<Entity> Entity
            {
                get { return Lite.Parse(Element.Current.ItemStatus); }
            }
        }

        public ListViewItemProxy ElementAt(int index)
        {
            var rows = GetRows();

            if (rows.Count <= index)
                throw new IndexOutOfRangeException("Row with index {0} not found, only {1} results on SearchControl {2}".FormatWith(index, rows.Count, Element.Current.Name));

            var row = rows[index];
            return row;
        }

        public PaginationSelectorProxy PaginationSelector
        {
            get { return new PaginationSelectorProxy(Element.ChildById("paginationSelector"), this); }
        }

        public AutomationElement ConstructFrom(OperationSymbol operationSymbol, int? timeOut = null)
        {
            var time = timeOut ?? OperationTimeouts.ConstructFromTimeout;

            return Element.CaptureWindow(
                () => GetOperationButton(operationSymbol).ButtonInvoke(),
                () => "Finding a window after {0} from SearchControl {1} took more than {2} ms".FormatWith(operationSymbol, QueryName, time), timeOut);
        }

        public string QueryNameKey
        {
            get { return Element.Current.Name; }
        }

        public object QueryName
        {
            get { return QueryLogic.QueryNames[QueryNameKey]; }
        }

        public NormalWindowProxy<T> ConstructFrom<F, T>(ConstructSymbol<T>.From<F> symbol, int? timeOut = null)
            where T : Entity
            where F : class, IEntity
        {
            AutomationElement element = ConstructFrom(symbol.Symbol, timeOut);

            return new NormalWindowProxy<T>(element);
        }

        public AutomationElement GetOperationButton(OperationSymbol operationSymbol)
        {
            return Element.Child(a => a.Current.ControlType == ControlType.Button && a.Current.Name == operationSymbol.Key);
        }

        public AutomationElement TryGetOperationButton(OperationSymbol operationSymbol)
        {
            return Element.TryChild(a => a.Current.ControlType == ControlType.Button && a.Current.Name == operationSymbol.Key);
        }
    }

    public class PaginationSelectorProxy
    {
        public AutomationElement Element { get; private set; }
        public SearchControlProxy SeachControl { get; private set; }

        public PaginationSelectorProxy(AutomationElement element, SearchControlProxy sc)
        {
            this.Element = element;
            this.SeachControl = sc;
        }

        public void PreviousPage()
        {
            Element.ChildById("btPrevious").ButtonInvoke();
        }

        public void NextPage()
        {
            Element.ChildById("btNext").ButtonInvoke();
        }

        public int CurrentPage
        {
            get { return int.Parse(Element.Child(a => a.Current.ControlType == ControlType.Text).Current.Name); }
            set { Element.Child(a => a.Current.ControlType == ControlType.Button && a.Current.Name == value.ToString()).ButtonInvoke(); }
        }

        public string GetNumResults()
        {
            return Element.ChildById("elementsInPageLabel").Child(a => a.Current.ControlType == ControlType.Text).Current.Name;
        }

        public AutomationElement GetElementsCombo()
        {
            return Element.ChildById("cbElements");
        }

        public int Elements
        {
            get { return int.Parse(GetElementsCombo().ComboGetSelectedItem().Current.Name); }
            set
            {
                GetElementsCombo().ComboSelectItem(ae => ae.Current.Name == value.ToString());

                SeachControl.WaitSearch();
            }
        }

        public AutomationElement GetPaginationModeCombo()
        {
            return Element.ChildById("cbMode");
        }

        public PaginationMode PaginationMode
        {
            get { return GetPaginationModeCombo().ComboGetSelectedItem().Current.Name.ToEnum<PaginationMode>(); }
            set
            {
                GetPaginationModeCombo().ComboSelectItem(ae => ae.Current.Name == value.ToString());

                SeachControl.WaitSearch();
            }
        }
    }

    public class FilterOptionProxy
    {
        public AutomationElement Element { get; private set; }
        SearchControlProxy searchControl;

        public FilterOptionProxy(AutomationElement element, SearchControlProxy searchControl)
        {
            this.Element = element;
            this.searchControl = searchControl;
        }

        public void SetOperation(FilterOperation operation)
        {
            Element.Child(e => e.Current.ControlType == ControlType.ComboBox).ComboSelectItem(a => a.Current.Name == operation.ToString());
        }

        public void SetValueString(string value)
        {
            var valueControl = new TreeWalker(ConditionBuilder.ToCondition(a => a.Current.ControlType == ControlType.Custom)).GetLastChild(Element);

            switch (valueControl.Current.ClassName)
            {
                case "ValueLine":
                    new ValueLineProxy(valueControl, null).StringValue = value;
                    break;

                case "EntityLine":
                    new EntityLineProxy(valueControl, null).Autocomplete(value);
                    break;
                default: throw new InvalidOperationException();
            }
        }

        public void Remove()
        {
            Element.ChildById("btRemove").ButtonInvoke();
        }
    }

    public static class SearchControlExtensions
    {
        public static SearchControlProxy GetSearchControl(this AutomationElement element)
        {
            var sc = element.Descendant(a => a.Current.ClassName == "SearchControl");

            return new SearchControlProxy(sc);
        }

        public static SearchControlProxy GetSearchControl(this AutomationElement element, object queryName)
        {
            var sc = element.Descendant(a => a.Current.ClassName == "SearchControl" && a.Current.Name == QueryUtils.GetKey(queryName));

            return new SearchControlProxy(sc);
        }

        public static SearchWindowProxy ToSearchWindow(this AutomationElement element)
        {
            return new SearchWindowProxy(element);
        }

    }
}
