using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public interface IEntityButtonContainer
    {
        ISelenium Selenium { get; }

        string Prefix { get; }

        bool HasChanges();

        string ButtonLocator(string buttonId);
    }

    public static class EntityButtonContainerExtensions
    {
        public static bool OperationEnabled(this IEntityButtonContainer container, Enum operationKey)
        {
            return container.ButtonEnabled(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static bool ButtonEnabled(this IEntityButtonContainer container, string idButton)
        {
            string locator = container.ButtonLocator(idButton);

            if (!container.Selenium.IsElementPresent(locator))
                throw new InvalidOperationException("{0} not found".Formato(idButton));

            return container.Selenium.IsElementPresent(locator + ":not(.sf-disabled)");
        }

        public static void ExecuteAjax(this IEntityButtonContainer container, Enum operationKey)
        {
            container.ButtonClick(operationKey.GetType().Name + "_" + operationKey.ToString());
            container.Selenium.Wait(() => !container.HasChanges());
        }

        public static void ExecuteSubmit(this IEntityButtonContainer container, Enum operationKey)
        {
            container.ButtonClick(operationKey.GetType().Name + "_" + operationKey.ToString());
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
            container.Selenium.Click(container.ButtonLocator(idButton) + ":not(.sf-disabled)");
        }

        public static string MenuOptionLocator(this IEntityButtonContainer container, string menuId, string optionId)
        {
            return "jq={0} #{1}.sf-dropdown ul.sf-menu-button li.ui-menu-item a.sf-entity-button#{2}".Formato(container.Prefix, menuId, optionId);
        }

        public static void ConstructFrom(this IEntityButtonContainer container, Enum operationKey)
        {
            container.MenuOption("tmConstructors", operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static NormalPage<T> ConstructFromNormalPage<T>(this IEntityButtonContainer container, Enum operationKey) where T: IdentifiableEntity
        {
            container.ConstructFrom(operationKey);
            return new NormalPage<T>(container.Selenium, null);
        }

        public static PopupControl<T> ConstructFromPopup<T>(this IEntityButtonContainer container, Enum operationKey) where T : ModifiableEntity
        {
            container.ConstructFrom(operationKey); 

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

            return container.Selenium.IsElementPresent(locator + ":not(.sf-disabled)");
        }

    }
}
