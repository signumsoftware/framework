using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Entities;
using Signum.Utilities;
using Signum.React.Selenium;

namespace Signum.React.Selenium
{
    public interface IEntityButtonContainer : ILineContainer
    {
        EntityInfoProxy EntityInfo();

        IWebElement ContainerElement();
    }

    public interface IEntityButtonContainer<T> : IEntityButtonContainer
    {
    }

    public static class EntityButtonContainerExtensions
    {
        public static Lite<T> GetLite<T>(this IEntityButtonContainer<T> container) where T : Entity
        {
            return (Lite<T>)container.EntityInfo().ToLite();
        }

        public static WebElementLocator OperationButton(this IEntityButtonContainer container, OperationSymbol symbol)
        {
            return container.ContainerElement().WithLocator(By.CssSelector($"button[data-operation='{symbol.Key}']"));
        }

        public static WebElementLocator OperationButton<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
            where T : Entity
        {
            return container.OperationButton(symbol.Symbol);
        }

        public static bool OperationEnabled(this IEntityButtonContainer container, OperationSymbol symbol)
        {
            return container.OperationButton(symbol).Find().GetAttribute("disabled") == null;
        }

        public static bool OperationEnabled<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
            where T : Entity
        {
            return container.OperationEnabled(symbol.Symbol);
        }

        public static bool OperationDisabled(this IEntityButtonContainer container, OperationSymbol symbol)
        {
            return container.OperationButton(symbol).Find().GetAttribute("disabled") != null;
        }

        public static bool OperationDisabled<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
              where T : Entity
        {
            return container.OperationDisabled(symbol.Symbol);
        }

        public static void OperationClick(this IEntityButtonContainer container, OperationSymbol symbol)
        {
            container.OperationButton(symbol).Find().ButtonClick();
        }
        
        public static void OperationClick<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
              where T : Entity
        {
            container.OperationClick(symbol.Symbol);
        }

        public static IWebElement OperationClickCapture(this IEntityButtonContainer container, OperationSymbol symbol)
        {
            return container.OperationButton(symbol).Find().CaptureOnClick();
        }

        public static IWebElement OperationClickCapture<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
              where T : Entity
        {
            return container.OperationClickCapture(symbol.Symbol);
        }

        public static void Execute<T>(this IEntityButtonContainer<T> container, ExecuteSymbol<T> symbol, bool consumeAlert = false)
            where T : Entity
        {
            container.WaitReload(() =>
            {
                container.OperationClick(symbol);
                if (consumeAlert)
                    container.Element.GetDriver().ConsumeAlert();
            });
        }

        public static void WaitReload(this IEntityButtonContainer container, Action action)
        {
            var ticks = container.TestTicks().Value;
            action();
            container.Element.GetDriver().Wait(() => container.TestTicks().Let(t => t != null && t != ticks));
        }

        public static void Delete<T>(this PopupFrame<T> container, DeleteSymbol<T> symbol, bool consumeAlert = true)
              where T : Entity
        {
            container.OperationClick(symbol);
            if (consumeAlert)
                container.Selenium.ConsumeAlert();

            container.WaitNotVisible();
        }

        public static PopupFrame<T> ConstructFrom<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
            where T : Entity
            where F : Entity
        {
            var element = container.OperationClickCapture(symbol);

            return new PopupFrame<T>(element).WaitLoaded();
        }

        public static PageFrame<T> ConstructFromNormalPage<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
            where T : Entity
            where F : Entity
        {
            container.OperationClick(symbol);

            container.Element.GetDriver().Wait(() => { try { return container.EntityInfo().IsNew; } catch { return false; } });

            return new PageFrame<T>(container.Element.GetDriver());
        }

        public static long? TestTicks(this IEntityButtonContainer container)
        {
            try
            {
                return container.Element.FindElement(By.CssSelector("div.sf-main-control[data-test-ticks]")).GetAttribute("data-test-ticks").ToLong();
            }
            catch
            {
                return null;
            }
        }

    }
}
