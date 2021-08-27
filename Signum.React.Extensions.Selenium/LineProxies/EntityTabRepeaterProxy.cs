using System;
using OpenQA.Selenium;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public class EntityTabRepeaterProxy : EntityBaseProxy
    {
        public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

        public EntityTabRepeaterProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public WebElementLocator Tab(int index) => new WebElementLocator(this.Element, By.CssSelector($".nav-tabs .nav-item.nav-link[data-rb-event-key=\"{index}\"]"));
        public WebElementLocator ElementPanel() => new WebElementLocator(this.Element, By.CssSelector($".sf-repeater-element.active"));

        public LineContainer<T> SelectTab<T>(int index) where T : ModifiableEntity
        {
            Tab(index).Find().Click();

            this.Element.GetDriver().Wait(() =>
            {
                var elem = this.ElementPanel().TryFind();
                return elem != null && elem.GetID().EndsWith("-" + index);
            });

            return this.Details<T>();
        }

        public int SelectedTabIndex()
        {
            var active = this.Element.FindElement(By.CssSelector(".nav-tabs .nav-item.nav-link.active"));

            return int.Parse(active.GetAttribute("data-rb-event-key"));
        }

        public LineContainer<T> Details<T>() where T : ModifiableEntity
        {
            return new LineContainer<T>(this.ElementPanel().WaitPresent(), this.ItemRoute);
        }
    }
}
