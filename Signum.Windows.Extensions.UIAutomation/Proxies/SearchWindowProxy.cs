using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

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

        public void Search()
        {
            SearchControl.Search();
        }

        public NormalWindowProxy ViewElementAt(int index)
        {
            return SearchControl.ViewElementAt(index); 
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

            var filterLine =  tw.GetLastChild(GetFilterBuilder());

            if (filterLine == null)
                throw new ElementNotAvailableException("Last FilterLine not found");

            return new FilterOptionProxy(filterLine);
        }

        public AutomationElement GetFilterBuilder()
        {
            return Element.ChildById("filterBuilder");
        }

        public void Search()
        {
            Element.ChildById("btFind").ButtonInvoke();

            Element.Wait(
                () => Element.ChildById("btFind").Current.IsEnabled,
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

            var win = Element.GetModalWindowAfter(() => Element.ChildById("btCreateColumn").ButtonInvoke(),
                () => "Adding new column for {0} on SearchControl {1}".Formato(token, Element.Current.ItemStatus));

            using(WindowProxy wp = new WindowProxy(win))
            {
                if (displayName != null)
                    wp.Element.Descendant(a => a.Current.ControlType == ControlType.Edit).Pattern<ValuePattern>().SetValue(displayName);

                wp.Element.ChildById("btAccept").ButtonInvoke();
            }
        }

        public NormalWindowProxy View(int? timeOut = null)
        {
            var win = Element.GetWindowAfter(
                () => Element.ChildById("btView").ButtonInvoke(),
                () => "View selected entity on SearchControl ({0})".Formato(Element.Current.ItemStatus), timeOut);

            return new NormalWindowProxy(win);
        }

        public NormalWindowProxy Create()
        {
            return new NormalWindowProxy(CreateBasic());
        }

        private AutomationElement CreateBasic(int? timeOut = null)
        {
            var win = Element.GetWindowAfter(
                () => Element.ChildById("btCreate").ButtonInvoke(),
                () => "Create a new entity on SearchControl ({0})".Formato(Element.Current.ItemStatus), timeOut);
            return win;
        }

        public AutomationElement Menu
        {
            get { return Element.ChildById("menu"); }
        }


        public AutomationElement Results
        {
            get { return Element.ChildById("lvResult"); }
        }

        public List<AutomationElement> GetRows()
        {
            return Results.Children(c => c.Current.ControlType == ControlType.DataItem);
        }

        public NormalWindowProxy ViewElementAt(int index)
        {
            var rows = GetRows();

            if(rows.Count <= index)
                throw new IndexOutOfRangeException("Row with index {0} not found, only {1} results on SearchControl {2}".Formato(index, rows.Count, Element.Current.ItemStatus));

            rows[index].Pattern<SelectionItemPattern>().Select();

            return View();
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
            get { return new PagerProxy(Element.ChildById("pageSelector"), this); }
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
            }
        }
    }

    public class PagerProxy
    {
        public AutomationElement Element { get; private set; }

        public PagerProxy(AutomationElement element, SearchControlProxy searchControl)
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

        public FilterOptionProxy(AutomationElement element)
        {
            this.Element = element;
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
                    new ValueLineProxy(valueControl).Value = value;
                    break;

                case "EntityLine":
                    new EntityLineProxy(valueControl).AutoComplete(value);
                    break;
                default: throw new InvalidOperationException();
            }
        }

        public void Remove()
        {
            Element.ChildById("btRemove").ButtonInvoke();
        }
    }
}
