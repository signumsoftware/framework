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
using Signum.Entities.Alerts;

namespace Signum.Web.Alerts
{
    public static class AlertWidgetHelper
    {
        public static AlertDN CreateAlert(IdentifiableEntity entity)
        {
            if(entity.IsNew)
                return null;

            return new AlertDN { Target = entity.ToLite() };
        }

        public static int CountAlerts(IdentifiableEntity identifiable, string filterField)
        {
            return Navigator.QueryCount(new CountOptions(typeof(AlertDN))
            {
                FilterOptions = 
                {
                    new FilterOption("Target", identifiable),
                    new FilterOption("Entity." + filterField, true) 
                },
            });
        }

        public static string JsOnAlertCreated(string prefix, string successMessage)
        {
            return "SF.Widgets.onAlertCreated('{0}','{1}','{2}')".Formato(
                RouteHelper.New().Action<AlertController>(wc => wc.AlertsCount()),
                prefix,
                successMessage);
        }

        public static WidgetItem CreateWidget(IdentifiableEntity identifiable, string partialViewName, string prefix)
        {
            if (identifiable == null || identifiable.IsNew || identifiable is AlertDN)
                return null;

            var alertList = new[]
            {
                new { Count = CountAlerts(identifiable, "Attended"), Query = "Attended", AlertClass = "sf-alert-attended", Title = Properties.Resources.Alerts_Attended },
                new { Count = CountAlerts(identifiable, "Alerted"), Query = "Alerted", AlertClass = "sf-alert-warned", Title = Properties.Resources.Alerts_NotAttended },
                new { Count = CountAlerts(identifiable, "Future"), Query = "Future", AlertClass = "sf-alert-future", Title = Properties.Resources.Alerts_Future },
            };

            JsViewOptions voptions = new JsViewOptions
            {
                Type = typeof(AlertDN).Name,
                Prefix = prefix,
                ControllerUrl = RouteHelper.New().Action<AlertController>(ac => ac.CreateAlert(prefix)),
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
                            .Attr("onclick", JsFindNavigator.openFinder(GetJsFindOptions(identifiable, a.Query, prefix)).ToJS())
                            .InnerHtml(
                            new HtmlTag("span").Class("sf-alert-count-label").Class(a.AlertClass).Class(a.Count > 0 ? "sf-alert-active" : null).InnerHtml((a.Title + ": ").EncodeHtml()),
                            new HtmlTag("span").Class(a.AlertClass).Class(a.Count > 0 ? "sf-alert-active" : null).SetInnerText(a.Count.ToString()))
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
                        .Class("sf-widget-count")
                        .Class(a.AlertClass)
                        .Class(a.Count > 0 ? "sf-alert-active" : null)
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

        static JsFindOptions GetJsFindOptions(IdentifiableEntity identifiable, string fieldToFilter, string prefix)
        {
            return new JsFindOptions
            {
                Prefix = TypeContextUtilities.Compose(prefix, "New"),
                FindOptions = new FindOptions
                {
                    QueryName = typeof(AlertDN),
                    Create = false,
                    SearchOnLoad = true,
                    FilterMode = FilterMode.AlwaysHidden,
                    FilterOptions = 
                    { 
                        new FilterOption("Target", identifiable.ToLite()),
                        new FilterOption("Entity." + fieldToFilter, true),
                    },
                    ColumnOptions = { new ColumnOption("Target") },
                    ColumnOptionsMode = ColumnOptionsMode.Remove,
                }
            };
        }
    }
}
