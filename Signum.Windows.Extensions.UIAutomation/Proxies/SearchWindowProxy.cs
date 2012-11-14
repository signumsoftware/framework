using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Operations;
using Signum.Engine.Basics;
using System.Windows;

namespace Signum.Windows.UIAutomation
{
    public class SearchWindowProxy : WindowProxy
    {
        public SearchControlProxy SearchControl { get; private set; }

        public SearchWindowProxy(AutomationElement element)
            : base(element)
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

        public void WaitSearch()
        {
            SearchControl.WaitSearch();
        }

        public NormalWindowProxy<T> Create<T>() where T : IdentifiableEntity
        {
            return SearchControl.Create<T>();
        }

        public NormalWindowProxy<T> ViewElementAt<T>(int index) where T : IdentifiableEntity
        {
            return SearchControl.ViewElementAt<T>(index); 
        }

        public void SelectElementAt(int index)
        {
            SearchControl.SelectElementAt(index);
        }

        public AutomationElement OkButton
        {
            get { return Element.ChildById("btOk"); }
        }

        public AutomationElement CanelButton
        {
            get { return Element.ChildById("btCancel"); }
        }

        public void Ok()
        {
            OkButton.ButtonInvoke();
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
                }, () => "Finding combo for token " + token[i] + (i == 0 ? "" : " after token {0}".Formato(token[i - 1])));

                combos[i].ComboSelectItem(a => a.Current.ItemStatus == tokens[i]);
            }
        }

        public void AddFilter()
        {
            Element.ChildById("btCreateFilter").ButtonInvoke();
        }

        public FilterOptionProxy LastFilter()
        {
            TreeWalker tw = new TreeWalker(ConditionBuilder.ToCondition(ae => ae.Current.ClassName == "FilterLine"));

            var filterLine =  tw.GetLastChild(FilterBuilderControl);

            if (filterLine == null)
                throw new ElementNotAvailableException("Last FilterLine not found");

            return new FilterOptionProxy(filterLine, this);
        }

        public AutomationElement FilterBuilderControl
        {
            get{ return Element.ChildById("filterBuilder");}
        }

        public AutomationElement SearchButton
        {
            get{ return Element.ChildById("btFind");}
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
                () =>{
                        Element.AssertMessageBoxChild();
                        return SearchButton.Current.IsEnabled;
                     },
                () => "Waiting after search on SearchControl {0}".Formato(Element.Current.ItemStatus));
        }

        public FilterOptionProxy AddFilterString(string token, FilterOperation operation, string value)
        {
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
                () => "Adding new column for {0} on SearchControl {1}".Formato(token, Element.Current.ItemStatus));

            using(WindowProxy wp = new WindowProxy(win))
            {
                if (displayName != null)
                    wp.Element.Descendant(a => a.Current.ControlType == ControlType.Edit).Pattern<ValuePattern>().SetValue(displayName);

                wp.Element.ChildById("btAccept").ButtonInvoke();
            }
        }

        public NormalWindowProxy<T> View<T>(int? timeOut = null) where T : IdentifiableEntity
        {
            var win = Element.CaptureWindow(
                () => ViewButton.ButtonInvoke(),
                () => "View selected entity on SearchControl ({0})".Formato(Element.Current.ItemStatus), timeOut);

            return new NormalWindowProxy<T>(win);
        }

        public NormalWindowProxy<T> Create<T>() where T:IdentifiableEntity
        {
            return new NormalWindowProxy<T>(CreateCapture());
        }

        public AutomationElement CreateCapture(int? timeOut = null)
        {
            var win = Element.CaptureWindow(
                () => CreateButton.ButtonInvoke(),
                () => "Create a new entity on SearchControl ({0})".Formato(Element.Current.ItemStatus), timeOut);

            return win;
        }

        public AutomationElement CreateButton
        {
            get{return Element.ChildById("btCreate");}
        }

        public AutomationElement ViewButton
        {
            get { return Element.ChildById("btView"); }
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
            var columnHeader = ResultColumns.Child(a => a.Current.ControlType == ControlType.HeaderItem && a.Current.ItemStatus == token);

            columnHeader.ButtonInvoke();

            while (columnHeader.Current.HelpText != orderType.ToString())
                columnHeader.ButtonInvoke();

            WaitSearch();
        }

        public List<AutomationElement> GetRows()
        {
            return Results.Children(c => c.Current.ControlType == ControlType.DataItem);
        }

        public bool HasRows()
        {
            return Results.TryChild(c => c.Current.ControlType == ControlType.DataItem) != null;
        }

        public NormalWindowProxy<T> ViewElementAt<T>(int index) where T : IdentifiableEntity
        {
            SelectElementAt(index);

            return View<T>();
        }

        public void SelectElementAt(int index)
        {
            var rows = GetRows();

            if (rows.Count <= index)
                throw new IndexOutOfRangeException("Row with index {0} not found, only {1} results on SearchControl {2}".Formato(index, rows.Count, Element.Current.ItemStatus));

            rows[index].Pattern<SelectionItemPattern>().Select();
        }

        public string GetNumResults()
        {
            return Element.ChildById("elementsInPageLabel").Child(a => a.Current.ControlType == ControlType.Text).Current.Name;
        }

        public AutomationElement GetElementsPerPageCombo()
        {
            return Element.ChildById("pageSizeSelector").Child(a => a.Current.ControlType == ControlType.ComboBox);
        }

        public PagerProxy Pager
        {
            get { return new PagerProxy(Element.ChildById("pageSelector")); }
        }

        public int? ElementsPerPage
        {
            get { return GetElementsPerPageCombo().ComboGetSelectedItem().Current.Name.ToInt(); }
            set
            {
                var combo = GetElementsPerPageCombo();
                combo.Pattern<ExpandCollapsePattern>().Expand();

                var item = value != null ? combo.Child(a => a.Current.Name == value.Value.ToString()) : combo.ChildrenAll().Last();

                item.Pattern<SelectionItemPattern>().Select();

                WaitSearch();
            }
        }

        public AutomationElement ConstructFrom(Enum operationKey, int? timeOut = null)
        {
            var time = timeOut ?? OperationTimeouts.ConstructFromTimeout;

            return Element.CaptureWindow(
                () => GetOperationButton(operationKey).ButtonInvoke(),
                () => "Finding a window after {0} from SearchControl {1} took more than {2} ms".Formato(OperationDN.UniqueKey(operationKey), QueryName, time), timeOut);
        }

        public string QueryNameKey
        {
            get { return Element.Current.ItemStatus; }
        }

        public object QueryName
        {
            get { return QueryLogic.QueryNames[QueryNameKey]; }
        }

        public NormalWindowProxy<T> ConstructFrom<T>(Enum operationKey, int? timeOut = null) where T : IdentifiableEntity
        {
            AutomationElement element = ConstructFrom(operationKey, timeOut);

            return new NormalWindowProxy<T>(element);
        }

        public AutomationElement GetOperationButton(Enum operationKey)
        {
            return Element.Child(a => a.Current.ItemStatus == OperationDN.UniqueKey(operationKey));
        }

        public void ValidateHeadersItems()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var columnHeader in HeaderItems)
	        {
                try
                {
                    columnHeader.ButtonInvoke();
                    WaitSearch();
                }
                catch (MessageBoxErrorException e)
                {
                    sb.AppendFormat("{0} ==> {1}\r\n", columnHeader.Current.ItemStatus, e.Message);
                }
	        }

            if (sb.Length > 0)
                throw new MessageBoxErrorException(sb.ToString());
        
        }
    }

    public class PagerProxy
    {
        public AutomationElement Element { get; private set; }

        public PagerProxy(AutomationElement element)
        {
            this.Element = element;
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
                    new EntityLineProxy(valueControl, null).AutoComplete(value);
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
            var sc = element.Descendant(a => a.Current.ClassName == "SearchControl" && a.Current.ItemStatus == QueryUtils.GetQueryUniqueKey(queryName));

            return new SearchControlProxy(sc);
        }
    }
}
