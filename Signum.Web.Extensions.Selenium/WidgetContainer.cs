using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Entities.Alerts;
using Signum.Entities.Notes;
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

            return "jq=#divMainPage ul.sf-widgets";
        }



        public static void QuickLinkClick(this IWidgetContainer container, string name)
        {
            container.Selenium.Click("{0} .sf-quicklinks".Formato(container.WidgetContainerLocator()));

            string quickLinkSelector = "{0} ul li.sf-quick-link[data-name='{1}'] > a".Formato(container.WidgetContainerLocator(), name);
            container.Selenium.WaitElementPresent("{0}:visible".Formato(quickLinkSelector));
            container.Selenium.Click(quickLinkSelector);
        }

        public static SearchPopupProxy QuickLinkClickSearch(this IWidgetContainer container, string name)
        {
            container.QuickLinkClick(name);
            var result = new SearchPopupProxy(container.Selenium, "_".Combine(container.Prefix, "New"));
            container.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            result.SearchControl.WaitInitialSearchCompleted();
            return result;
        }

        public static PopupControl<NoteDN> NotesCreateClick(this IWidgetContainer container)
        {
            container.Selenium.Click("{0} .sf-notes-toggler".Formato(container.WidgetContainerLocator()));

            string createSelector = "{0} a.sf-note-create".Formato(container.WidgetContainerLocator());
            container.Selenium.WaitElementPresent("{0}:visible".Formato(createSelector));
            container.Selenium.MouseUp(createSelector);

            PopupControl<NoteDN> result = new PopupControl<NoteDN>(container.Selenium, "New");
            container.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            return result;
        }

        public static SearchPopupProxy NotesViewClick(this IWidgetContainer container)
        {
            container.Selenium.Click("{0} .sf-notes-toggler".Formato(container.WidgetContainerLocator()));

            string viewSelector = "{0} a.sf-note-view".Formato(container.WidgetContainerLocator());
            container.Selenium.WaitElementPresent("{0}:visible".Formato(viewSelector));
            container.Selenium.MouseUp(viewSelector);

            SearchPopupProxy result = new SearchPopupProxy(container.Selenium, "New");
            container.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            result.SearchControl.WaitInitialSearchCompleted();
            return result;
        }

        public static int NotesCount(this IWidgetContainer container)
        {
            string str = container.Selenium.GetEval("window.$('{0} .sf-notes-toggler .sf-widget-count').html()".Formato(container.WidgetContainerLocator().RemoveStart(3)));

            return int.Parse(str); 
        }

        public static PopupControl<AlertDN> AlertCreateClick(this IWidgetContainer container)
        {
            container.Selenium.Click("{0} .sf-alerts-toggler".Formato(container.WidgetContainerLocator()));

            string createSelector = "{0} a.sf-alert-create".Formato(container.WidgetContainerLocator());
            container.Selenium.WaitElementPresent("{0}:visible".Formato(createSelector));
            container.Selenium.MouseUp(createSelector);

            PopupControl<AlertDN> result = new PopupControl<AlertDN>(container.Selenium, "New");
            container.Selenium.WaitElementPresent(result.PopupVisibleLocator);
            return result;
        }


        public static SearchPopupProxy AlertsViewClick(this IWidgetContainer container, AlertCurrentState state)
        {
            container.Selenium.Click("{0} .sf-alerts-toggler".Formato(container.WidgetContainerLocator()));

            string viewSelector = "{0} .sf-alert-view .{1}.sf-alert-count-label".Formato(
                container.WidgetContainerLocator(),
                GetCssClass(state));

            container.Selenium.WaitElementPresent(viewSelector + ":visible");
            container.Selenium.MouseUp(viewSelector);

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

            throw new InvalidOperationException("Unexpected state {0}".Formato(state)); 
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
            var result = container.Selenium.GetEval("window.$('{0} span.sf-widget-count.{1}').html()".Formato(
                container.WidgetContainerLocator().RemoveStart(3),
                GetCssClass(state)));

            return int.Parse(result);
        }
    }
}
