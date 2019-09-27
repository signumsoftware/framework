using System;
using OpenQA.Selenium;
using Signum.Entities;

namespace Signum.React.Selenium
{
    public class EntityTabRepeaterProxy : EntityBaseProxy
    {
        public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

        public EntityTabRepeaterProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public WebElementLocator Tab(int index) => new WebElementLocator(this.Element, By.CssSelector($".nav-tabs li[data-eventkey=\"{index}\"]"));
        public WebElementLocator ElementPanel() => new WebElementLocator(this.Element, By.CssSelector($".sf-repeater-element"));

        public LineContainer<T> SelectTab<T>(int index) where T : ModifiableEntity
        {
            Tab(index).Find().Click();

            return this.Details<T>();
        }

        public int SelectedTabIndex()
        {
            var active = this.Element.FindElement(By.CssSelector(".nav-tabs .nav-item .nav-link.active"));

            return int.Parse(active.GetParent().GetAttribute("data-eventkey"));
        }

        public LineContainer<T> Details<T>() where T : ModifiableEntity
        {
            return new LineContainer<T>(this.ElementPanel().WaitPresent(), this.ItemRoute);
        }
    }
}
