using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public interface IEntityButtonContainer : ILineContainer
    {
        RuntimeInfoProxy RuntimeInfo();

        By ContainerLocator();
    }

    public interface IEntityButtonContainer<T> : IEntityButtonContainer
    {
    }

    public static class EntityButtonContainerExtensions
    {
        public static Lite<T> GetLite<T>(this IEntityButtonContainer<T> container) where T : Entity
        {
            return (Lite<T>)container.RuntimeInfo().ToLite();
        }

        public static By ButtonLocator(this IEntityButtonContainer container, string buttonId)
        {
            return container.ContainerLocator().CombineCss(" #" + "_".CombineIfNotEmpty(container.Prefix, buttonId));
        }

        public static By OperationLocator<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
            where T : Entity
        {
            return container.ButtonLocator(symbol.Symbol.KeyWeb());
        }

        public static string KeyWeb(this OperationSymbol operationSymbol)
        {
            return operationSymbol.Key.Replace('.', '_');
        }

        public static bool ButtonEnabled(this IEntityButtonContainer container, string idButton)
        {
            By locator = container.ButtonLocator(idButton);

            return container.Selenium.IsElementPresent(locator.CombineCss(":not([disabled])"));
        }

        public static bool OperationEnabled<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
            where T : Entity
        {
            return container.ButtonEnabled(symbol.Symbol.KeyWeb());
        }

        public static bool ButtonDisabled(this IEntityButtonContainer container, string idButton)
        {
            By locator = container.ButtonLocator(idButton);

            return container.Selenium.IsElementPresent(locator.CombineCss("[disabled]"));
        }

        public static bool OperationDisabled<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
              where T : Entity
        {
            return container.ButtonDisabled(symbol.Symbol.KeyWeb());
        }

        public static void ButtonClick(this IEntityButtonContainer container, string idButton)
        {
            var button = container.Selenium.FindElement(container.ButtonLocator(idButton));

            button.ButtonClick();
        }


        public static void OperationClick<T>(this IEntityButtonContainer<T> container, IEntityOperationSymbolContainer<T> symbol)
              where T : Entity
        {
            container.ButtonClick(symbol.Symbol.KeyWeb());
        }

        public static void ExecuteAjax<T>(this IEntityButtonContainer<T> container, ExecuteSymbol<T> symbol, bool consumeAlert = false)
            where T : Entity
        {
            container.WaitReload(() =>
            {
                container.OperationClick(symbol);
                if (consumeAlert)
                    container.Selenium.ConsumeAlert();
            });
        }

        public static void WaitReload(this IEntityButtonContainer container, Action action)
        {
            var ticks = container.TestTicks().Value;
            action();
            container.Selenium.Wait(() => container.TestTicks().Let(t => t != null && t != ticks));
        }

        public static void ExecuteSubmit<T>(this NormalPage<T> container, ExecuteSymbol<T> symbol, bool consumeAlert = false)
              where T : Entity
        {
            container.OperationClick(symbol);
            if (consumeAlert)
                container.Selenium.ConsumeAlert();
            container.WaitLoadedAndId();
        }

        public static SearchPageProxy DeleteSubmit<T>(this NormalPage<T> container, DeleteSymbol<T> symbol, bool consumeAlert = true)
         where T : Entity
        {
            container.OperationClick(symbol);
            if (consumeAlert)
                container.Selenium.ConsumeAlert();

            return new SearchPageProxy(container.Selenium).WaitLoaded();
        }

        public static void DeleteAjax<T>(this PopupControl<T> container, DeleteSymbol<T> symbol, bool consumeAlert = true)
              where T : Entity
        {
            container.OperationClick(symbol);
            if (consumeAlert)
                container.Selenium.ConsumeAlert();

            container.WaitNotVisible();
        }

        public static NormalPage<T> ConstructFromNormalPageSaved<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
            where T : Entity
            where F : Entity
        {
            container.OperationClick(symbol);

            return new NormalPage<T>(container.Selenium, null).WaitLoaded();
        }

        public static NormalPage<T> ConstructFromNormalPageNew<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol)
            where T : Entity
            where F : Entity
        {
            container.OperationClick(symbol);

            container.Selenium.Wait(() => { try { return container.RuntimeInfo().IsNew; } catch { return false; } });

            return new NormalPage<T>(container.Selenium, null);
        }

        public static NormalPage<T> OperationNormalPageNew<T>(this IEntityButtonContainer container, IOperationSymbolContainer symbol)
            where T : Entity
        {
            container.ButtonClick(symbol.Symbol.KeyWeb());

            container.Selenium.Wait(() => { try { return container.RuntimeInfo().IsNew; } catch { return false; } });

            return new NormalPage<T>(container.Selenium, null);
        }

        public static PopupControl<T> OperationPopup<T>(this IEntityButtonContainer container, IOperationSymbolContainer symbol, string prefix = "New")
            where T : ModifiableEntity
        {
            container.ButtonClick(symbol.Symbol.KeyWeb());

            var popup = new PopupControl<T>(container.Selenium, prefix);

            container.Selenium.WaitElementPresent(popup.PopupLocator);

            return popup;
        }

        public static PopupControl<T> ConstructFromPopup<F, T>(this IEntityButtonContainer<F> container, ConstructSymbol<T>.From<F> symbol, string prefix = "New")
            where T : Entity
            where F : Entity
        {
            container.OperationClick(symbol);

            var popup = new PopupControl<T>(container.Selenium, prefix);

            container.Selenium.WaitElementPresent(popup.PopupLocator);

            return popup;
        }

        public static bool HasChanges(this IEntityButtonContainer container)
        {
            return container.Selenium.IsElementPresent(By.CssSelector("#{0}divMainControl.sf-changed".FormatWith(container.PrefixUnderscore())));
        }

        public static long? TestTicks(this IEntityButtonContainer container)
        {
            try
            {
                return ((string)container.Selenium.ExecuteScript("return $ && $('#" + container.PrefixUnderscore() + "divMainControl').attr('data-test-ticks')")).ToLong();
            }
            catch
            {
                return null;
            }
        }

    }
}
