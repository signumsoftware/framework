using System;
using OpenQA.Selenium;
using Signum.Entities;

namespace Signum.React.Selenium
{
    public class EntityDetailProxy : EntityBaseProxy
    {
        public EntityDetailProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public Lite<IEntity> Lite
        {
            get { return EntityInfo()?.ToLite(); }
            set
            {
                if (this.EntityInfo() != null)
                    this.Remove();

                if (this.FindButton.IsVisible())
                    this.Find().SelectLite(value);

                throw new NotImplementedException("AutoComplete");
            }
        }

        public LineContainer<T> Details<T>() where T : ModifiableEntity
        {
            return new LineContainer<T>(this.Element.FindElement(By.CssSelector("div[data-property-path]")), Route);
        }

        public EntityInfoProxy EntityInfo()
        {
            return EntityInfoInternal(null);
        }

        public ILineContainer<T> GetOrCreateDetailControl<T>() where T : ModifiableEntity
        {
            if (this.EntityInfo() !=null)
                return this.Details<T>();

            CreateEmbedded<T>();

            return this.Details<T>();
        }
    }
}
