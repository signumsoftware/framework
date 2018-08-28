using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using Signum.Entities.DynamicQuery;

namespace Signum.React.Selenium
{
    public class FiltersProxy
    {
        public IWebElement Element { get; private set; }

        public FiltersProxy(IWebElement element)
        {
            this.Element = element;
        }

        public IEnumerable<IWebElement> Filters()
        {
            return Element.FindElements(By.CssSelector("table.sf-filter-table > tbody > tr"));
        }

        public FilterConditionOptionProxy GetNewFilter(Action action)
        {
            var oldFilters = this.Filters();
            action();
            var newFilter = this.Element.GetDriver().Wait(() => this.Filters().Except(oldFilters).SingleOrDefault(), () => "new filter to appear");

            return new FilterConditionOptionProxy(newFilter);
        }

        public WebElementLocator AddFilterButton
        {
            get { return this.Element.WithLocator(By.ClassName("sf-line-button sf-create")); }
        }

        public FilterConditionOptionProxy AddFilter()
        {
            return GetNewFilter(() => this.AddFilterButton.Find().Click());
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

        public FilterConditionOptionProxy GetFilter(int index)
        {
            return new FilterConditionOptionProxy(this.Filters().ElementAt(index));
        }


    }
}
