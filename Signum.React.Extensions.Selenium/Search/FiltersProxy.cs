using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public class FiltersProxy
    {
        public IWebElement Element { get; private set; }

        public FiltersProxy(IWebElement element)
        {
            this.Element = element;
        }

        public IEnumerable<FilterProxy> Filters()
        {
            return Element.FindElements(By.XPath("table/tbody/tr")).Select(a =>
                a.HasClass("sf-filter-condition") ? new FilterConditionProxy(a): 
                a.HasClass("sf-filter-group") ? new FilterGroupProxy(a) : (FilterProxy?)null).NotNull().ToList();
        }

        public FilterProxy GetNewFilter(Action action)
        {
            var oldFilters = this.Filters();
            action();
            var newFilter = this.Element.GetDriver().Wait(() => this.Filters().Skip(oldFilters.Count()).SingleOrDefault(), () => "new filter to appear");

            return newFilter;
        }

        public WebElementLocator AddFilterButton
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-line-button.sf-create-condition")); }
        }

        public WebElementLocator AddGroupButton
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-line-button.sf-create-group")); }
        }

        public FilterConditionProxy AddFilter()
        {
            return (FilterConditionProxy)GetNewFilter(() => this.AddFilterButton.Find().Click());
        }

        public FilterGroupProxy AddGroup()
        {
            return (FilterGroupProxy)GetNewFilter(() => this.AddGroupButton.Find().Click());
        }

        public void AddFilter(string token, FilterOperation operation, object value)
        {
            var fo = this.AddFilter();
            fo.QueryToken.SelectToken(token);
            fo.Operation = operation;
            fo.SetValue(value);
        }

        public bool IsAddFilterEnabled
        {
            get { return this.AddFilterButton.CombineCss(":not([disabled])").IsPresent(); }
        }

        public FilterProxy GetFilter(int index)
        {
            return this.Filters().ElementAt(index);
        }


    }
}
