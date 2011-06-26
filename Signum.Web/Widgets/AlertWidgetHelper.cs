using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Web.Properties;
using Signum.Web.Controllers;
using System.Web.Mvc;

namespace Signum.Web
{
    public static class AlertWidgetHelper
    {
        public static Type AlertType { get; set; }
        public static Func<IdentifiableEntity, IAlertDN> CreateAlert { get; set; }
        public static string AlertsQueryColumn { get; set; }
        
        public static object WarnedAlertsQuery { get; set; }
        public static object AttendedAlertsQuery { get; set; }
        public static object FutureAlertsQuery { get; set; }

        public static int CountAlerts(IdentifiableEntity identifiable, object queryName)
        {
            return Navigator.QueryCount(new CountOptions(queryName)
            {
                FilterOptions = { new FilterOption(AlertsQueryColumn, identifiable) },
            });
        }

        public static string JsOnAlertCreated(string prefix, string successMessage)
        {
            return "SF.Widgets.onAlertCreated('{0}','{1}','{2}')".Formato(
                RouteHelper.New().Action<WidgetsController>(wc => wc.AlertsCount()),
                prefix,
                successMessage);
        }

        public static WidgetItem CreateWidget(HtmlHelper helper, IdentifiableEntity identifiable, string partialViewName, string prefix)
        {
            if (identifiable == null || identifiable.IsNew || identifiable is IAlertDN)
                return null;

            var alertList = new[]
            {
                new { Count = CountAlerts(identifiable, WarnedAlertsQuery), Query = WarnedAlertsQuery, Class = "sf-alert-warned", Title = Properties.Resources.Alerts_NotAttended },
                new { Count = CountAlerts(identifiable, FutureAlertsQuery), Query = FutureAlertsQuery, Class = "sf-alert-future", Title = Properties.Resources.Alerts_Future },
                new { Count = CountAlerts(identifiable, AttendedAlertsQuery), Query = AttendedAlertsQuery, Class = "sf-alert-attended", Title = Properties.Resources.Alerts_Attended },
            };

            JsViewOptions voptions = new JsViewOptions
            {
                Type = AlertType.Name,
                Prefix = prefix,
                ControllerUrl = RouteHelper.New().Action("CreateAlert", "Widgets"),
                RequestExtraJsonData = "function(){{ return {{ {0}: new SF.RuntimeInfo('{1}').find().val() }}; }}".Formato(EntityBaseKeys.RuntimeInfo, prefix),
                OnOkClosed = new JsFunction() { JsOnAlertCreated(prefix, Resources.AlertCreated) }
            };

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-menu-button sf-widget-content sf-alerts")))
            {
                foreach (var a in alertList)
                {
                    using (content.Surround(new HtmlTag("li").Class("sf-alert")))
                    {
                        content.AddLine(new HtmlTag("a")
                            .Class("sf-alert-view")
                            .Attr("onclick", new JsFindNavigator(GetJsFindOptions(identifiable, a.Query)).openFinder().ToJS())
                            .InnerHtml(
                                new HtmlTag("span").Class("sf-alert-count-label " + a.Class).InnerHtml((a.Title + ": ").EncodeHtml()),
                                new HtmlTag("span").Class(a.Class).SetInnerText(a.Count.ToString()))
                            .ToHtml());
                    }
                }

                content.Add(new HtmlTag("hr").ToHtmlSelf());

                using (content.Surround(new HtmlTag("li").Class("sf-alert")))
                {
                    content.AddLine(new HtmlTag("a")
                       .Class("sf-alert-create")
                       .Attr("onclick", new JsViewNavigator(voptions).createSave(RouteHelper.New().SignumAction("TrySavePartial")).ToJS())
                       .InnerHtml(Resources.CreateAlert.EncodeHtml())
                       .ToHtml());
                }
            }

            HtmlStringBuilder label = new HtmlStringBuilder();
            var toggler = new HtmlTag("a")
                .Class("sf-widget-toggler sf-alerts-toggler")
                .Attr("title", Resources.Alerts);
            using (label.Surround(toggler))
            {
                label.Add(new HtmlTag("span")
                    .Class("ui-icon ui-icon-calendar")
                    .InnerHtml(Resources.Notes.EncodeHtml())
                    .ToHtml());

                int count = alertList.Length;
                for(int i = 0; i < count; i++)
                {
                    var a = alertList[i];
                    
                    label.Add(new HtmlTag("span")
                        .Class("sf-widget-count " + a.Class)
                        .SetInnerText(a.Count.ToString())
                        .Attr("title", a.Title)
                        .ToHtml());

                    if (i < count - 1)
                    {
                        label.Add(new HtmlTag("span")
                            .Class("sf-alerts-count-separator")
                            .SetInnerText(" - ")
                            .ToHtml());
                    }
                }
            }

            return new WidgetItem
            {
                Id = TypeContextUtilities.Compose(prefix, "alertsWidget"),
                Label = label.ToHtml(),
                Content = content.ToHtml()
            };
        }

        static JsFindOptions GetJsFindOptions(IdentifiableEntity identifiable, object queryName)
        {
            return new JsFindOptions
            {
                FindOptions = new FindOptions
                {
                    QueryName = queryName,
                    Create = false,
                    SearchOnLoad = true,
                    FilterMode = FilterMode.AlwaysHidden,
                    FilterOptions = { new FilterOption(AlertsQueryColumn, identifiable.ToLite()) },
                    ColumnOptions = { new ColumnOption( AlertsQueryColumn) },
                    ColumnOptionsMode = ColumnOptionsMode.Remove,
                }
            };
        }
    }
}
