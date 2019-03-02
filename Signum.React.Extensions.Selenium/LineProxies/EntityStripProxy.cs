using OpenQA.Selenium;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public class EntityStripProxy : EntityBaseProxy
    {
        public EntityStripProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public WebElementLocator ItemsContainerElement
        {
            get { return this.Element.WithLocator(By.CssSelector("ul.sf-strip")); }
        }

        public WebElementLocator StripItemSelector(int index)
        {
            return this.ItemsContainerElement.CombineCss(" > li.sf-strip-element:nth-child({0})".FormatWith(index + 1));
        }

        public int ItemsCount()
        {
            return this.ItemsContainerElement.CombineCss(" > li.sf-strip-element").FindElements().Count;
        }

        public WebElementLocator ViewElementIndex(int index)
        {
            return StripItemSelector(index).CombineCss(" > a.sf-entitStrip-link");
        }

        public WebElementLocator RemoveElementIndex(int index)
        {
            return StripItemSelector(index).CombineCss(" > a.sf-remove");
        }

        public void Remove(int index)
        {
            RemoveElementIndex(index).Find().Click();
        }

        public EntityInfoProxy EntityInfo(int index)
        {
            return EntityInfoInternal(index);
        }

        public WebElementLocator AutoCompleteElement
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-typeahead")); }
        }

        public void AutoComplete(Lite<IEntity> lite)
        {
            base.AutoCompleteWaitChanges(AutoCompleteElement.Find(), lite);
        }

        public FrameModalProxy<T> View<T>(int index) where T : ModifiableEntity
        {
            var changes = this.GetChanges();
            var popup = ViewElementIndex(index).Find().CaptureOnClick();

            return new FrameModalProxy<T>(popup, this.ItemRoute)
            {
                Disposing = okPressed => WaitNewChanges(changes, "create dialog closed")
            };
        }
    }
}
