using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities.DynamicQuery;

namespace Signum.Windows.UIAutomation
{
    public class SearchWindowProxy:WindowProxy
    {
        public SearchControlProxy SearchControl { get; private set; }

        public SearchWindowProxy(AutomationElement owner)
            : base(owner)
        {
            SearchControl = new SearchControlProxy(owner.ChildById("searchControl"));
        }

        public void AddFilter(string token, FilterOperation operation, object value)
        {
            SearchControl.SelectToken(token);

            SearchControl.AddFilter();

            var lastFilter = SearchControl.LastFilter();



            return null;
        }

        public void AddColumn(string token, string displayName)
        {
            SearchControl.AddColumn();
        }

        public void Search()
        {
            SearchControl.Search();
        }

        public NormalWindowProxy OpenItemByIndex(int index)
        {
            return SearchControl.OpenItemByIndex(index); 
        }
    }

    public class SearchControlProxy
    {
        public AutomationElement Owner { get; private set; }

        public SearchControlProxy(AutomationElement automationElement)
        {
            this.Owner = automationElement;
        }

        public void SelectToken(string token)
        {
            var tokens = token.Split('.');

            var tb = Owner.ChildById("tokenBuilder");

            var tw = new TreeWalker(FindExtensions.ToCondition(a => a.Current.ControlType == ControlType.ComboBox));
            var combo = tw.GetFirstChild(tb);

            foreach (var t in tokens)
            {
                combo.Pattern<ExpandCollapsePattern>().Expand();

                var item = combo.Child(a => a.Current.ItemStatus == t);

                item.Pattern<SelectionItemPattern>().Select();

                combo = tw.GetNextSibling(combo);
            }
        }


        public void AddFilter()
        {
            Owner.ChildById("btCreateFilter").Pattern<InvokePattern>().Invoke();
        }

        public AutomationElement LastFilter()
        {
            TreeWalker tw = new TreeWalker(FindExtensions.ToCondition(ae => ae.Current.ClassName == "FilterLine"));

            var fileLine =  tw.GetLastChild(GetFilterBuilder());

            if (fileLine == null)
                throw new ElementNotAvailableException("Last FilterLine not found");

            return fileLine;
        }

        public AutomationElement GetFilterBuilder()
        {
            return Owner.ChildById("filterBuilder");
        }

        public void AddColumn()
        {
            Owner.ChildById("btCreateColumn").Pattern<InvokePattern>().Invoke();
        }

        public void Search()
        {
            Owner.ChildById("btFind").Pattern<InvokePattern>().Invoke();
        }

        public NormalWindowProxy OpenItemByIndex(int index)
        {
            throw new NotImplementedException();
        }
    }

    public class FilterOptionProxy
    {
        public AutomationElement Owner { get; private set; }

        public FilterOptionProxy(AutomationElement automationElement)
        {
            this.Owner = automationElement;
        }

        public void SetOperation(FilterOperation fo)
        {

        }


    }
}
