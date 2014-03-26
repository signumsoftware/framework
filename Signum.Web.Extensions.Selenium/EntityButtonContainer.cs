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

    public static class EntityButtonContainerExtensions
    {
        public static string ButtonLocator(this IEntityButtonContainer container, string buttonId)
        {
            return container.ContainerLocator() + " #{0}.sf-entity-button".Formato(buttonId);
        }

        public static string OperationLocator(this IEntityButtonContainer container, Enum operationKey)
        {
            return container.ButtonLocator(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static bool OperationEnabled(this IEntityButtonContainer container, Enum operationKey)
        {
            return container.ButtonEnabled(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static bool OperationDisabled(this IEntityButtonContainer container, Enum operationKey)
        {
            return container.ButtonEnabled(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static bool ButtonEnabled(this IEntityButtonContainer container, string idButton)
        {
            string locator = container.ButtonLocator(idButton);

            return container.Selenium.IsElementPresent(locator + ":not([disabled])");
        }

        public static bool ButtonDisabled(this IEntityButtonContainer container, string idButton)
        {
            string locator = container.ButtonLocator(idButton);

            return container.Selenium.IsElementPresent(locator + "[disabled]");
        }

        public static void OperationClick(this IEntityButtonContainer container, Enum operationKey)
        {
            container.ButtonClick(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static void ExecuteAjax(this IEntityButtonContainer container, Enum operationKey)
        {
            container.WaitReload(() => container.OperationClick(operationKey));
        }

        public static void WaitReload(this IEntityButtonContainer container, Action action)
        {
            var ticks = container.TestTicks().Value;
            action();
            container.Selenium.Wait(() => container.TestTicks().Let(t => t != null && t != ticks));
        }

        public static void ExecuteSubmit(this IEntityButtonContainer container, Enum operationKey)
        {
            container.OperationClick(operationKey);
            container.Selenium.WaitForPageToLoad();
        }

        public static SearchPageProxy DeleteSubmit(this IEntityButtonContainer container, Enum operationKey)
        {
            container.ExecuteSubmit(operationKey);
            container.Selenium.ConsumeConfirmation();

            container.Selenium.WaitForPageToLoad(); 

            return new SearchPageProxy(container.Selenium);
        }

        public static void ButtonClick(this IEntityButtonContainer container, string idButton)
        {
            container.Selenium.Click(container.ButtonLocator(idButton) + ":not([disabled])");
        }

        public static string MenuOptionLocator(this IEntityButtonContainer container, string menuId, string optionId)
        {
            return container.ContainerLocator() + " #{0}".Formato(optionId);
        }

        public static void ConstructFrom(this IEntityButtonContainer container, Enum operationKey, string group = "tmConstructors")
        {
            if (group.HasText())
                container.MenuOption(group, operationKey.GetType().Name + "_" + operationKey.ToString());
            else
                container.ButtonClick(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static NormalPage<T> ConstructFromNormalPageSaved<T>(this IEntityButtonContainer container, Enum operationKey, string group = "tmConstructors") where T: IdentifiableEntity
        {
            container.ConstructFrom(operationKey, group);

            container.Selenium.WaitForPageToLoad();

            return new NormalPage<T>(container.Selenium, null);
        }

        public static NormalPage<T> ConstructFromNormalPageNew<T>(this IEntityButtonContainer container, Enum operationKey, string group = "tmConstructors") where T : IdentifiableEntity
        {
            container.ConstructFrom(operationKey, group);

            container.Selenium.Wait(() => container.RuntimeInfo().IsNew);

            return new NormalPage<T>(container.Selenium, null);
        }

        public static PopupControl<T> ConstructFromPopup<T>(this IEntityButtonContainer container, Enum operationKey, string group = "tmConstructors") where T : ModifiableEntity
        {
            container.ConstructFrom(operationKey, group); 

            var popup = new PopupControl<T>(container.Selenium, "New");

            container.Selenium.WaitElementPresent(popup.PopupVisibleLocator);

            return popup;
        }

        public static void MenuOption(this IEntityButtonContainer container, string menuId, string optionId)
        {
            container.Selenium.Click(container.MenuOptionLocator(menuId, optionId));
        }

        public static bool ConstructFromEnabled(this IEntityButtonContainer container, Enum constructFromKey)
        {
            return container.MenuOptionEnabled("tmConstructors", constructFromKey.GetType().Name + "_" + constructFromKey.ToString());
        }

        public static bool MenuOptionEnabled(this IEntityButtonContainer container, string menuId, string optionId)
        {
            string locator = container.MenuOptionLocator(menuId, optionId);

            if (!container.Selenium.IsElementPresent(locator))
                throw new InvalidOperationException("{0} not found on {1}".Formato(optionId, menuId));

            return container.Selenium.IsElementPresent(locator + ":not([disabled])");
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
