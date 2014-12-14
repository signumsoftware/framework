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
            container.Selenium.FindElement(container.WidgetContainerLocator().CombineCss(" .sf-quicklinks"));

            By quickLinkSelector = container.WidgetContainerLocator().CombineCss(" ul li.sf-quick-link[data-name='{0}'] > a".FormatWith(name));
            container.Selenium.WaitElementPresent(quickLinkSelector);
            container.Selenium.FindElement(quickLinkSelector).ButtonClick();
        }

        public static SearchPopupProxy QuickLinkClickSearch(this IWidgetContainer container, string name)
        {
            container.QuickLinkClick(name);
            var result = new SearchPopupProxy(container.Selenium, "_".Combine(container.Prefix, "New"));
            container.Selenium.WaitElementPresent(result.PopupLocator);
            result.SearchControl.WaitInitialSearchCompleted();
            return result;
        }

        public static PopupControl<NoteEntity> NotesCreateClick(this IWidgetContainer container)
        {
            container.Selenium.FindElement(container.WidgetContainerLocator().CombineCss(" .sf-notes-toggler")).Click();

            By createLocator = container.WidgetContainerLocator().CombineCss(" a.sf-note-create");
            container.Selenium.WaitElementVisible(createLocator);
            container.Selenium.FindElement(createLocator).Click();

            PopupControl<NoteEntity> result = new PopupControl<NoteEntity>(container.Selenium, "New");
            container.Selenium.WaitElementPresent(result.PopupLocator);
            return result;
        }

        public static SearchPopupProxy NotesViewClick(this IWidgetContainer container)
        {
            container.Selenium.FindElement(container.WidgetContainerLocator().CombineCss(" .sf-notes-toggler")).Click();

            By viewSelector = container.WidgetContainerLocator().CombineCss(" a.sf-note-view");
            container.Selenium.WaitElementVisible(viewSelector);
            container.Selenium.FindElement(viewSelector).Click();

            SearchPopupProxy result = new SearchPopupProxy(container.Selenium, "New");
            container.Selenium.WaitElementPresent(result.PopupLocator);
            result.SearchControl.WaitInitialSearchCompleted();
            return result;
        }

        public static int NotesCount(this IWidgetContainer container)
        {
            string str = (string)container.Selenium.ExecuteScript("return $('{0} .sf-notes-toggler .sf-widget-count').html()".FormatWith(container.WidgetContainerLocator().CssSelector()));

            return int.Parse(str); 
        }

        public static PopupControl<AlertEntity> AlertCreateClick(this IWidgetContainer container)
        {
            container.Selenium.FindElement(container.WidgetContainerLocator().CombineCss(" .sf-alerts-toggler")).Click();

            By createLocator = container.WidgetContainerLocator().CombineCss(" a.sf-alert-create");
            container.Selenium.WaitElementVisible(createLocator);
            container.Selenium.FindElement(createLocator).Click();

            PopupControl<AlertEntity> result = new PopupControl<AlertEntity>(container.Selenium, "New");
            container.Selenium.WaitElementPresent(result.PopupLocator);
            return result;
        }


        public static SearchPopupProxy AlertsViewClick(this IWidgetContainer container, AlertCurrentState state)
        {
            container.Selenium.FindElement(container.WidgetContainerLocator().CombineCss(" .sf-alerts-toggler")).Click();

            By viewSelector = container.WidgetContainerLocator().CombineCss(" .sf-alert-view .{0}.sf-alert-count-label".FormatWith(GetCssClass(state)));

            container.Selenium.WaitElementVisible(viewSelector);
            container.Selenium.FindElement(viewSelector).Click();

            SearchPopupProxy result = new SearchPopupProxy(container.Selenium, "alerts");
            container.Selenium.WaitElementPresent(result.PopupLocator);
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
            var result = (string)container.Selenium.ExecuteScript("return $('{0} span.sf-widget-count.{1}').html()".FormatWith(
                container.WidgetContainerLocator().CssSelector(),
                GetCssClass(state)));

            return int.Parse(result);
        }
    }
}
