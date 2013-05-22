using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Selenium;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Signum.Web.Selenium
{
    public static class WidgetsTestExtensions
    {
        public static string WidgetContainerSelector(string prefix)
        {
            if (prefix.HasText())
            {
                throw new NotImplementedException("WidgetContainerSelector not implemented for popups");
            }
            else
                return "jq=#divNormalControl .sf-widgets-container";
        }

        public static void QuickLinkClick(this ISelenium selenium, int quickLinkIndexBase1)
        {
            QuickLinkClick(selenium, quickLinkIndexBase1, "");
        }

        public static void QuickLinkClick(this ISelenium selenium, int quickLinkIndexBase1, string prefix)
        {
            selenium.Click("{0} .sf-quicklink-toggler".Formato(WidgetContainerSelector(prefix)));

            string quickLinkSelector = "{0} .sf-quicklinks > .sf-quicklink:nth-child({1}) > a".Formato(WidgetContainerSelector(prefix), quickLinkIndexBase1);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:visible".Formato(quickLinkSelector)));
            selenium.Click(quickLinkSelector);
        }

        static string notesTogglerClass = "sf-notes-toggler";
        static string notesDropDownClass = "sf-notes";

        public static void NotesCreateClick(this ISelenium selenium)
        {
            NotesCreateClick(selenium, "");
        }

        public static void NotesCreateClick(this ISelenium selenium, string prefix)
        {
            selenium.Click("{0} .{1}".Formato(WidgetContainerSelector(prefix), notesTogglerClass));

            string createSelector = "{0} .{1} .sf-note-create".Formato(WidgetContainerSelector(prefix), notesDropDownClass);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:visible".Formato(createSelector)));
            selenium.Click(createSelector);
        }

        public static void NotesViewClick(this ISelenium selenium)
        {
            NotesViewClick(selenium, "");
        }

        public static void NotesViewClick(this ISelenium selenium, string prefix)
        {
            selenium.Click("{0} .{1}".Formato(WidgetContainerSelector(prefix), notesTogglerClass));

            string viewSelector = "{0} .{1} .sf-note-view".Formato(WidgetContainerSelector(prefix), notesDropDownClass);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:visible".Formato(viewSelector)));
            selenium.Click(viewSelector);
        }

        public static bool EntityHasNNotes(this ISelenium selenium, int notesNumber)
        {
            return EntityHasNNotes(selenium, notesNumber, "");
        }

        public static bool EntityHasNNotes(this ISelenium selenium, int notesNumber, string prefix)
        {
            return selenium.IsElementPresent("{0} .{1} .sf-widget-count:contains({2})".Formato(
                WidgetContainerSelector(prefix), 
                notesTogglerClass,
                notesNumber));
        }

        static string alertsTogglerClass = "sf-alerts-toggler";
        static string alertsDropDownClass = "sf-alerts";

        public static string AlertWarnedClass = "sf-alert-warned";
        public static string AlertFutureClass = "sf-alert-future";
        public static string AlertAttendedClass = "sf-alert-attended";

        public static void AlertsCreateClick(this ISelenium selenium)
        {
            AlertsCreateClick(selenium, "");
        }

        public static void AlertsCreateClick(this ISelenium selenium, string prefix)
        {
            selenium.Click("{0} .{1}".Formato(WidgetContainerSelector(prefix), alertsTogglerClass));

            string createSelector = "{0} .{1} .sf-alert-create".Formato(WidgetContainerSelector(prefix), alertsDropDownClass);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:visible".Formato(createSelector)));
            selenium.Click(createSelector);
        }

        public static void AlertsViewClick(this ISelenium selenium, string alertTypeClass)
        {
            AlertsViewClick(selenium, alertTypeClass, "");
        }

        public static void AlertsViewClick(this ISelenium selenium, string alertTypeClass, string prefix)
        {
            selenium.Click("{0} .{1}".Formato(WidgetContainerSelector(prefix), alertsTogglerClass));

            string viewSelector = "{0} .{1} .sf-alert-view .{2}".Formato(
                WidgetContainerSelector(prefix),
                alertsDropDownClass,
                alertTypeClass);

            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:visible".Formato(viewSelector)));
            selenium.Click(viewSelector);
        }

        public static bool EntityHasNAlerts(this ISelenium selenium, int alertsNumber, string alertTypeClass)
        {
            return EntityHasNAlerts(selenium, alertsNumber, alertTypeClass, "");
        }

        public static bool EntityHasNAlerts(this ISelenium selenium, int alertsNumber, string alertTypeClass, string prefix)
        {
            return selenium.IsElementPresent("{0} .{1} .sf-widget-count.{2}:contains({3})".Formato(
                WidgetContainerSelector(prefix),
                alertsTogglerClass,
                alertTypeClass,
                alertsNumber));
        }
    }
}
