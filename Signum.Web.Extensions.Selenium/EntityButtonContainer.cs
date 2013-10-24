using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public interface IEntityButtonContainer
    {
        ISelenium Selenium { get; }

        string Prefix { get; }
    }

    public static class EntityButtonContainerExtensions
    {
        public static string ButtonLocator(this IEntityButtonContainer container, string buttonId)
        {
            return "jq={0} #{1}.sf-entity-button".Formato(container.Prefix, buttonId);
        }

        public static bool ExecuteEnabled(this IEntityButtonContainer container, Enum operationKey)
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

        public static void ExecuteClick(this IEntityButtonContainer container, Enum operationKey)
        {
            container.ButtonClick(operationKey.GetType().Name + "_" + operationKey.ToString());
        }

        public static void ButtonClick(this IEntityButtonContainer container, string idButton)
        {
            container.Selenium.Click(container.ButtonLocator(idButton) + ":not(.sf-disabled)");
        }

        public static string MenuOptionLocator(this IEntityButtonContainer container, string menuId, string optionId)
        {
            return "jq={0} #{1}.sf-dropdown ul.sf-menu-button li.ui-menu-item a.sf-entity-button#{2}".Formato(container.Prefix, menuId, optionId);
        }

        public static void ConstructFromClick(this IEntityButtonContainer container, Enum constructFromKey)
        {
            container.MenuOptionClick("tmConstructors", constructFromKey.GetType().Name + "_" + constructFromKey.ToString());
        }

        public static void MenuOptionClick(this IEntityButtonContainer container, string menuId, string optionId)
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
