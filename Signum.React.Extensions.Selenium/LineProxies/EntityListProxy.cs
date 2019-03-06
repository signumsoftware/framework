using OpenQA.Selenium;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public class EntityListProxy : EntityBaseProxy
    {
        public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

        public EntityListProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public WebElementLocator OptionElement(int index)
        {
            return this.ListElement.Find().WithLocator(By.CssSelector("option:nth-child({0})".FormatWith(index + 1)));
        }

        public WebElementLocator ListElement
        {
            get { return this.Element.WithLocator(By.CssSelector("select.form-control")); }
        }

        public void Select(int index)
        {
            this.OptionElement(index).Find().Click();
        }

        public FrameModalProxy<T> View<T>(int index) where T : ModifiableEntity
        {
            Select(index);

            return base.ViewInternal<T>();
        }

        public int ItemsCount()
        {
            return this.ListElement.Find().FindElements(By.CssSelector("option")).Count;
        }

        public EntityInfoProxy EntityInfo(int index)
        {
            return EntityInfoInternal(index);
        }

        public void DoubleClick(int index)
        {
            Select(index);
            OptionElement(index).Find().DoubleClick();
        }
    }
}
