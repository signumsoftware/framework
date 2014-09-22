using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public interface IEntityButtonContainer : ILineContainer
    {
        RuntimeInfoProxy RuntimeInfo();

        string ContainerLocator();
    }

    public interface IEntityButtonContainer<T> : IEntityButtonContainer
    {
    }

    public static class EntityButtonContainerExtensions
    {
        public static Lite<T> GetLite<T>(this IEntityButtonContainer<T> container) where T : IdentifiableEntity
        {
            return (Lite<T>)container.RuntimeInfo().ToLite();
        }

        public static string ButtonLocator(this IEntityButtonContainer container, string buttonId)
        {
            return container.ContainerLocator() + " #" + "_".CombineIfNotEmpty(container.Prefix, buttonId);
        }

        public static string OperationLocator<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
            where T : IdentifiableEntity
        {
            return container.ButtonLocator(symbol.Symbol.KeyWeb());
        }

        public static string KeyWeb(this OperationSymbol operationSymbol)
        {
            return operationSymbol.Key.Replace('.', '_');
        }

        public static bool ButtonEnabled(this IEntityButtonContainer container, string idButton)
        {
            string locator = container.ButtonLocator(idButton);

            return container.Selenium.IsElementPresent(locator + ":not([disabled])");
        }

        public static bool OperationEnabled<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
            where T : IdentifiableEntity
        {
            return container.ButtonEnabled(symbol.Symbol.KeyWeb());
        }

        public static bool ButtonDisabled(this IEntityButtonContainer container, string idButton)
        {
            string locator = container.ButtonLocator(idButton);

            return container.Selenium.IsElementPresent(locator + "[disabled]");
        }

        public static bool OperationDisabled<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
              where T : IdentifiableEntity
        {
            return container.ButtonDisabled(symbol.Symbol.KeyWeb());
        }

        public static void ButtonClick(this IEntityButtonContainer container, string idButton)
        {
            container.Selenium.MouseUp(container.ButtonLocator(idButton) + ":not([disabled])");
        }

        public static void OperationClick<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
              where T : IdentifiableEntity
        {
            container.ButtonClick(symbol.Symbol.KeyWeb());
        }

        public static void ExecuteAjax<T>(this IEntityButtonContainer<T> container, ExecuteSymbol<T> symbol)
            where T : IdentifiableEntity
        {
            container.WaitReload(() => container.OperationClick(symbol));
        }

        public static void WaitReload(this IEntityButtonContainer container, Action action)
        {
            var ticks = container.TestTicks().Value;
            action();
            container.Selenium.Wait(() => container.TestTicks().Let(t => t != null && t != ticks));
        }

        public static void ExecuteSubmit<T>(this IEntityButtonContainer<T> container, ExecuteSymbol<T> symbol)
              where T : IdentifiableEntity
        {
            container.OperationClick(symbol);
            container.Selenium.WaitForPageToLoad();
        }

        public static SearchPageProxy DeleteSubmit<T>(this IEntityButtonContainer<T> container, DeleteSymbol<T> symbol)
              where T : IdentifiableEntity
        {
            container.OperationClick(symbol);
            container.Selenium.ConsumeConfirmation();

            container.Selenium.WaitForPageToLoad();

            return new SearchPageProxy(container.Selenium);
        }

        public static NormalPage<T> ConstructFromNormalPageSaved<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
            where T : IdentifiableEntity
            where F : IdentifiableEntity
        {
            container.OperationClick(symbol);

            container.Selenium.WaitForPageToLoad();

            return new NormalPage<T>(container.Selenium, null);
        }

        public static NormalPage<T> ConstructFromNormalPageNew<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
            where T : IdentifiableEntity
            where F : IdentifiableEntity
        {
            container.OperationClick(symbol);

            container.Selenium.Wait(() => container.RuntimeInfo().IsNew);

            return new NormalPage<T>(container.Selenium, null);
        }

        public static NormalPage<T> OperationNormalPageNew<T>(this IEntityButtonContainer container, IOperationSymbolContainer symbol)
            where T : IdentifiableEntity
        {
            container.ButtonClick(symbol.Symbol.KeyWeb());

            container.Selenium.Wait(() => container.RuntimeInfo().IsNew);

            return new NormalPage<T>(container.Selenium, null);
        }

        public static PopupControl<T> OperationPopup<T>(this IEntityButtonContainer container, IOperationSymbolContainer symbol, string prefix = "New")
            where T : ModifiableEntity
        {
            container.ButtonClick(symbol.Symbol.KeyWeb());

            var popup = new PopupControl<T>(container.Selenium, prefix);

            container.Selenium.WaitElementPresent(popup.PopupVisibleLocator);

            return popup;
        }

        public static PopupControl<T> ConstructFromPopup<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
            where T : IdentifiableEntity
            where F : IdentifiableEntity
        {
            container.OperationClick(symbol);

            var popup = new PopupControl<T>(container.Selenium, "New");

            container.Selenium.WaitElementPresent(popup.PopupVisibleLocator);

            return popup;
        }

        public static bool HasChanges(this IEntityButtonContainer container)
        {
            return container.Selenium.IsElementPresent("jq=#{0}divMainControl.sf-changed".Formato(container.PrefixUnderscore()));
        }

        public static long? TestTicks(this IEntityButtonContainer container)
        {
            try
            {
                return container.Selenium.GetEval("window && window.$ && window.$('#" + container.PrefixUnderscore() + "divMainControl').attr('data-test-ticks')").ToLong();
            }
            catch
            {
                return null;
            }
        }

    }
}
