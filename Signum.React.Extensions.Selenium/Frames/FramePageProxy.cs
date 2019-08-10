using System;
using OpenQA.Selenium.Remote;
using Signum.Engine;
using Signum.Entities;
using OpenQA.Selenium;

namespace Signum.React.Selenium
{
    public class FramePageProxy<T> : ILineContainer<T>, IEntityButtonContainer<T>, IWidgetContainer, IValidationSummaryContainer, IDisposable where T : ModifiableEntity
    {
        public RemoteWebDriver Selenium { get; private set; }

        public IWebElement Element { get; private set; }

        public PropertyRoute Route { get; private set; }

        public FramePageProxy(RemoteWebDriver selenium)
        {
            this.Selenium = selenium;
            this.Element = selenium.WaitElementPresent(By.CssSelector(".normal-control"));
            this.Route = PropertyRoute.Root(typeof(T));
        }

        public IWebElement ContainerElement()
        {
            return this.Element;
        }

        public Action? OnDisposed;
        public void Dispose()
        {
            OnDisposed?.Invoke();
        }

        public WebElementLocator MainControl
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-main-control"));  }
        }

        public EntityInfoProxy EntityInfo()
        {
            var attr = MainControl.Find().GetAttribute("data-main-entity");

            return EntityInfoProxy.Parse(attr)!;
        }

        public T RetrieveEntity()
        {
            var lite = this.EntityInfo().ToLite();
            return (T)(IEntity)lite.RetrieveAndRemember();
        }

        public FramePageProxy<T> WaitLoaded()
        {
            MainControl.WaitPresent();
            return this;
        }
    }
}
