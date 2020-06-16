using System;
using OpenQA.Selenium;
using Signum.Entities;

namespace Signum.React.Selenium
{
    public class EntityLineProxy : EntityBaseProxy
    {
        public EntityLineProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public void SetLite(Lite<IEntity>? value)
        {
            if (this.EntityInfo() != null)
                this.Remove();

            if (value != null)
            {
                if (AutoCompleteElement.IsVisible())
                    AutoComplete(value);
                else if (FindButton != null)
                    this.Find().SelectLite(value);
                else
                    throw new NotImplementedException("AutoComplete");
            }
        }

        public Lite<Entity>? GetLite()
        {
            return EntityInfo()?.ToLite();
        }

        public WebElementLocator AutoCompleteElement
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-entity-autocomplete")); }
        }

        public void AutoComplete(Lite<IEntity> lite)
        {
            base.AutoCompleteWaitChanges(AutoCompleteElement.Find(), Element,  lite);
        }

        public void AutoCompleteBasic(Lite<IEntity> lite)
        {
            AutoCompleteBasic(AutoCompleteElement.Find(), Element, lite);
        }

        public FrameModalProxy<T> View<T>() where T : ModifiableEntity
        {
            return base.ViewInternal<T>();
        }

        public EntityInfoProxy? EntityInfo()
        {
            return EntityInfoInternal(null);
        }


    }
}
