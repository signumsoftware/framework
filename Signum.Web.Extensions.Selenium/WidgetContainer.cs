using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Entities.Alerts;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public interface IWidgetContainer
    {
        ISelenium Selenium { get; }

        string Prefix { get; }
    }

    public static class WidgetContainerExtensions
    {
        public static string WidgetContainerLocator(this IWidgetContainer container)
        {
            if (container.Prefix.HasText())
                throw new NotImplementedException("WidgetContainerSelector not implemented for popups");

            return "jq=#divNormalControl .sf-widgets-container";
        }



        public static void QuickLinkClick(this IWidgetContainer container, int quickLinkIndex)
        {
            container.Selenium.Click("{0} .sf-quicklink-toggler".Formato(container.WidgetContainerLocator()));

            string quickLinkSelector = "{0} .sf-quicklinks > .sf-quicklink:nth-child({1}) > a".Formato(container.WidgetContainerLocator(), quickLinkIndex + 1);
            container.Selenium.WaitAjaxFinished(() => container.Selenium.IsElementPresent("{0}:visible".Formato(quickLinkSelector)));
            container.Selenium.Click(quickLinkSelector);
        }



        public static void NotesCreateClick(this IWidgetContainer container)
        {
            container.Selenium.Click("{0} .sf-notes-toggler".Formato(container.WidgetContainerLocator()));

            string createSelector = "{0} .sf-notes .sf-note-create".Formato(container.WidgetContainerLocator());
            container.Selenium.WaitAjaxFinished(() => container.Selenium.IsElementPresent("{0}:visible".Formato(createSelector)));
            container.Selenium.Click(createSelector);
        }

        public static void NotesViewClick(this IWidgetContainer container)
        {
            container.Selenium.Click("{0} .sf-notes-toggler".Formato(container.WidgetContainerLocator()));

            string viewSelector = "{0} .sf-notes .sf-note-view".Formato(container.WidgetContainerLocator());
            container.Selenium.WaitAjaxFinished(() => container.Selenium.IsElementPresent("{0}:visible".Formato(viewSelector)));
            container.Selenium.Click(viewSelector);
        }

        public static bool HasCountNotes(this IWidgetContainer container, int countNotes)
        {
            return container.Selenium.IsElementPresent("{0} .sf-notes-toggler .sf-widget-count:contains({1})".Formato(
                container.WidgetContainerLocator(),
                countNotes));
        }

        public static void AlertsCreateClick(this IWidgetContainer container)
        {
            container.Selenium.Click("{0} .sf-alerts-toggler".Formato(container.WidgetContainerLocator()));

            string createSelector = "{0} .sf-alerts .sf-alert-create".Formato(container.WidgetContainerLocator());
            container.Selenium.WaitAjaxFinished(() => container.Selenium.IsElementPresent("{0}:visible".Formato(createSelector)));
            container.Selenium.Click(createSelector);
        }


        public static void AlertsViewClick(this IWidgetContainer container, AlertCurrentState state)
        {
            container.Selenium.Click("{0} .sf-alerts-toggler".Formato(container.WidgetContainerLocator()));

            string viewSelector = "{0} .sf-alerts .sf-alert-view .{1}".Formato(
                container.WidgetContainerLocator(),
                GetCssClass(state));

            container.Selenium.WaitAjaxFinished(() => container.Selenium.IsElementPresent(viewSelector + ":visible"));
            container.Selenium.Click(viewSelector);
        }

        static string GetCssClass(AlertCurrentState state)
        {
            if (state == AlertCurrentState.Future)
                return "sf-alert-future";

            if (state == AlertCurrentState.Alerted)
                return "sf-alert-warned";

            if(state == AlertCurrentState.Attended)
                return "sf-alert-attended";

            throw new InvalidOperationException("Unexpected state {0}".Formato(state)); 
        }

        public static bool EntityHasNAlerts(this IWidgetContainer container, int alertsNumber, AlertCurrentState state)
        {
            return container.Selenium.IsElementPresent("{0} .sf-alerts-toggler .sf-widget-count.{1}:contains({3})".Formato(
                container.WidgetContainerLocator(),
                GetCssClass(state),
                alertsNumber));
        }
    }
}
