using OpenQA.Selenium;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.React.Selenium
{

    public abstract class FilterProxy { }
    public class FilterGroupProxy : FilterProxy
    {
        public IWebElement Element;

        public FilterGroupProxy(IWebElement element)
        {
            this.Element = element;
        }
    }

    public class FilterConditionProxy : FilterProxy
    {
        public IWebElement Element;

        public FilterConditionProxy(IWebElement element)
        {
            this.Element = element;
        }

        public WebElementLocator DeleteButton
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-line-button.sf-remove")); }
        }

        public QueryTokenBuilderProxy QueryToken
        {
            get { return new QueryTokenBuilderProxy(this.Element.FindElement(By.ClassName("sf-query-token-builder"))); }
        }

        public WebElementLocator OperationElement
        {
            get { return this.Element.WithLocator(By.CssSelector("td.sf-filter-operation select")); }
        }

        public WebElementLocator ValueElement
        {
            get { return this.Element.WithLocator(By.CssSelector("tr.sf-filter-value *")); }
        }

        public FilterOperation Operation
        {
            get { return OperationElement.Find().SelectElement().SelectedOption.GetAttribute("value").ToEnum<FilterOperation>(); }
            set { OperationElement.Find().SelectElement().SelectByValue(value.ToString()); }
        }

        public void Delete()
        {
            DeleteButton.Find().Click();
        }

        public ValueLineProxy ValueLine()
        {
            return new ValueLineProxy(this.Element, null!);
        }

        public EntityLineProxy EntityLine()
        {
            return new EntityLineProxy(this.Element, null!);
        }

        internal void SetValue(object value)
        {
            if (value == null)
                return; //Hack

            if (value is Lite<Entity>)
                EntityLine().SetLite((Lite<Entity>)value);
            else if (value is Entity)
                EntityLine().SetLite(((Entity)value).ToLite());
            else
                ValueLine().SetStringValue(value.ToString());
        }
    }
}
