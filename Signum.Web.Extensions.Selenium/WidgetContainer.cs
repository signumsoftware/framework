using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Remote;
using Signum.Entities.Alerts;
using Signum.Entities.Notes;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public interface IWidgetContainer
    {
        RemoteWebDriver Selenium { get; }

        string Prefix { get; }
    }

    public static class WidgetContainerExtensions
    {
        public static By WidgetContainerLocator(this IWidgetContainer container)
        {
            if (container.Prefix.HasText())
                throw new NotImplementedException("WidgetContainerSelector not implemented for popups");

            return By.CssSelector("#divMainPage ul.sf-widgets");
        }



        public static void QuickLinkClick(this IWidgetContainer container, string name)
        {
            container.Selenium.FindElement(By.CssSelector("{0} .sf-quicklinks".FormatWith(container.WidgetContainerLocator())));

            By quickLinkSelector = By.CssSelector("{0} ul li.sf-quick-link[data-name='{1}'] > a".FormatWith(container.WidgetContainerLocator(), name));
            container.Selenium.WaitElementPresent(quickLinkSelector);
            container.Selenium.FindElement(quickLinkSelector).Click();
        }

        public static SearchPopupProxy QuickLinkClickSearch(this IWidgetContainer container, string name)
        {
            container.QuickLinkClick(name);
            var result = new SearchPopupProxy(container.Selenium, "_".Combine(container.Prefix, "New"));
            container.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            result.SearchControl.WaitInitialSearchCompleted();
            return result;
        }

        public static PopupControl<NoteEntity> NotesCreateClick(this IWidgetContainer container)
        {
            container.Selenium.FindElement(container.WidgetContainerLocator().CombineCss(" .sf-notes-toggler")).Click();

            string createSelector = "{0} a.sf-note-create".FormatWith(container.WidgetContainerLocator());
            container.Selenium.WaitElementPresent(By.CssSelector("{0}:visible".FormatWith(createSelector)));
            container.Selenium.FindElement(By.CssSelector(createSelector)).Click();

            PopupControl<NoteEntity> result = new PopupControl<NoteEntity>(container.Selenium, "New");
            container.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            return result;
        }

        public static SearchPopupProxy NotesViewClick(this IWidgetContainer container)
        {
            container.Selenium.FindElement(By.CssSelector("{0} .sf-notes-toggler".FormatWith(container.WidgetContainerLocator()))).Click();

            string viewSelector = "{0} a.sf-note-view".FormatWith(container.WidgetContainerLocator());
            container.Selenium.WaitElementPresent(By.CssSelector("{0}:visible".FormatWith(viewSelector)));
            container.Selenium.FindElement(By.CssSelector(viewSelector)).Click();

            SearchPopupProxy result = new SearchPopupProxy(container.Selenium, "New");
            container.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            result.SearchControl.WaitInitialSearchCompleted();
            return result;
        }

        public static int NotesCount(this IWidgetContainer container)
        {
            string str = (string)container.Selenium.ExecuteScript("window.$('{0} .sf-notes-toggler .sf-widget-count').html()".FormatWith(container.WidgetContainerLocator().CssSelector().RemoveStart(3)));

            return int.Parse(str); 
        }

        public static PopupControl<AlertEntity> AlertCreateClick(this IWidgetContainer container)
        {
            container.Selenium.FindElement(container.WidgetContainerLocator().CombineCss(" .sf-alerts-toggler")).Click();

            string createSelector = "{0} a.sf-alert-create".FormatWith(container.WidgetContainerLocator());
            container.Selenium.WaitElementPresent(By.CssSelector("{0}:visible".FormatWith(createSelector)));
            container.Selenium.FindElement(By.CssSelector(createSelector)).Click();

            PopupControl<AlertEntity> result = new PopupControl<AlertEntity>(container.Selenium, "New");
            container.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            return result;
        }


        public static SearchPopupProxy AlertsViewClick(this IWidgetContainer container, AlertCurrentState state)
        {
            container.Selenium.FindElement(By.CssSelector("{0} .sf-alerts-toggler".FormatWith(container.WidgetContainerLocator()))).Click();

            string viewSelector = "{0} .sf-alert-view .{1}.sf-alert-count-label".FormatWith(
                container.WidgetContainerLocator(),
                GetCssClass(state));

            container.Selenium.WaitElementPresent(By.CssSelector(viewSelector + ":visible"));
            container.Selenium.FindElement(By.CssSelector(viewSelector)).Click();

            SearchPopupProxy result = new SearchPopupProxy(container.Selenium, "alerts");
            container.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            result.SearchControl.WaitInitialSearchCompleted();
            return result;
        }

        static string GetCssClass(AlertCurrentState state)
        {
            if (state == AlertCurrentState.Future)
                return "sf-alert-future";

            if (state == AlertCurrentState.Alerted)
                return "sf-alert-alerted";

            if(state == AlertCurrentState.Attended)
                return "sf-alert-attended";

            throw new InvalidOperationException("Unexpected state {0}".FormatWith(state)); 
        }

        public static bool AlertsAre(this IWidgetContainer container, int attended, int alerted, int future)
        {
            return
                attended == container.AlertCount(AlertCurrentState.Attended) &&
                alerted == container.AlertCount(AlertCurrentState.Alerted) &&
                future == container.AlertCount(AlertCurrentState.Future);
        }
            

        public static int AlertCount(this IWidgetContainer container, AlertCurrentState state)
        {
            var result = (string)container.Selenium.ExecuteScript("window.$('{0} span.sf-widget-count.{1}').html()".FormatWith(
                container.WidgetContainerLocator().CssSelector().RemoveStart(3),
                GetCssClass(state)));

            return int.Parse(result);
        }
    }
}
