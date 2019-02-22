using System;
using OpenQA.Selenium;
using Signum.Entities;
using Signum.Utilities;

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

        public static void Execute<T>(this IEntityButtonContainer<T> container, ExecuteSymbol<T> symbol, bool consumeAlert = false, bool checkValidationErrors = true)
            where T : Entity
        {
            container.WaitReload(() =>
            {
                container.OperationClick(symbol);
                if (consumeAlert)
                    container.Element.GetDriver().CloseMessageModal(MessageModalButton.Yes);
            });

            var vs = container as IValidationSummaryContainer;
            if (checkValidationErrors && vs != null)
            {
                AssertNoErrors(vs);
            }
        }

        public static void WaitReload(this IEntityButtonContainer container, Action action)
        {
            var ticks = container.TestTicks().Value;
            action();
            container.Element.GetDriver().Wait(() => container.TestTicks().Let(t => t != null && t != ticks));
        }

        private static void AssertNoErrors(this IValidationSummaryContainer vs)
        {
            var errors = vs.ValidationErrors();

            if (!errors.IsNullOrEmpty())
                throw new InvalidOperationException("Validation Errors found: \r\n" + errors.ToString("\r\n").Indent(4));
        }

        public static void Delete<T>(this FrameModalProxy<T> container, DeleteSymbol<T> symbol, bool consumeAlert = true)
              where T : Entity
        {
            container.OperationClick(symbol);
            if (consumeAlert)
                container.Selenium.CloseMessageModal(MessageModalButton.Yes);

            container.WaitNotVisible();
        }

        public static FrameModalProxy<T> ConstructFrom<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
            where T : Entity
            where F : Entity
        {
            var element = container.OperationClickCapture(symbol);

            return new FrameModalProxy<T>(element).WaitLoaded();
        }

        public static FramePageProxy<T> ConstructFromNormalPage<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
            where T : Entity
            where F : Entity
        {
            container.OperationClick(symbol);

            container.Element.GetDriver().Wait(() => { try { return container.EntityInfo().IsNew; } catch { return false; } });

            return new FramePageProxy<T>(container.Element.GetDriver());
        }

        public static long? TestTicks(this IEntityButtonContainer container)
        {
            try
            {
                return container.Element.TryFindElement(By.CssSelector("div.sf-main-control[data-test-ticks]"))?.GetAttribute("data-test-ticks").ToLong();
            }
            catch
            {
                return null;
            }
        }

    }
}
