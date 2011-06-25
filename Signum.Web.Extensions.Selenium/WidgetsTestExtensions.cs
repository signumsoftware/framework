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
            selenium.MouseOver("{0} .sf-quicklink-toggler".Formato(WidgetContainerSelector(prefix)));
            selenium.Click("{0} .sf-quicklinks > .sf-quicklink:nth-child({1}) > a".Formato(WidgetContainerSelector(prefix), quickLinkIndexBase1));
        }

        static string notesTogglerClass = "sf-notes-toggler";
        static string notesDropDownClass = "sf-notes";

        public static void NotesCreateClick(this ISelenium selenium)
        {
            NotesCreateClick(selenium, "");
        }

        public static void NotesCreateClick(this ISelenium selenium, string prefix)
        {
            selenium.MouseOver("{0} .{1}".Formato(WidgetContainerSelector(prefix), notesTogglerClass));
            selenium.Click("{0} .{1} .sf-note-create".Formato(WidgetContainerSelector(prefix), notesDropDownClass));
        }

        public static void NotesViewClick(this ISelenium selenium)
        {
            NotesViewClick(selenium, "");
        }

        public static void NotesViewClick(this ISelenium selenium, string prefix)
        {
            selenium.MouseOver("{0} .{1}".Formato(WidgetContainerSelector(prefix), notesTogglerClass));
            selenium.Click("{0} .{1} .sf-note-view".Formato(WidgetContainerSelector(prefix), notesDropDownClass));
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
    }
}
