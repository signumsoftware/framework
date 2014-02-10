using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Web.Controllers;
using System.Web.Mvc;
using Signum.Entities.Alerts;
using Signum.Entities.Notes;
using Newtonsoft.Json.Linq;
using Signum.Engine.DynamicQuery;

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

        public static WidgetItem CreateWidget(IdentifiableEntity identifiable, string partialViewName, string prefix)
        {
            if (identifiable == null || identifiable.IsNew || identifiable is AlertDN)
                return null;

         

            var alertList = new[]
            {
                new { Count = CountAlerts(identifiable, "Attended"), Property = "Attended", AlertClass = "sf-alert-attended", Title = AlertMessage.Alerts_Attended.NiceToString() },
                new { Count = CountAlerts(identifiable, "Alerted"), Property = "Alerted", AlertClass = "sf-alert-alerted", Title = AlertMessage.Alerts_NotAttended.NiceToString() },
                new { Count = CountAlerts(identifiable, "Future"), Property = "Future", AlertClass = "sf-alert-future", Title = AlertMessage.Alerts_Future.NiceToString() },
            };

            var options = new FindOptions
            {
                QueryName = typeof(AlertDN),
                Create = false,
                SearchOnLoad = true,
                FilterMode = FilterMode.Hidden,
                FilterOptions = 
                { 
                    new FilterOption("Target", identifiable.ToLite()),
                    //new FilterOption("Entity." + fieldToFilter, true),
                },
                ColumnOptions = { new ColumnOption("Target") },
                ColumnOptionsMode = ColumnOptionsMode.Remove,
            }.ToJS(TypeContextUtilities.Compose(prefix, "New"));

            HtmlStringBuilder content = new HtmlStringBuilder();

            using (content.Surround(new HtmlTag("ul")
                .Attr("data-url",RouteHelper.New().Action((AlertController ac)=>ac.AlertsCount()))
                .Attr("data-findOptions", options.ToString())
                .Class("sf-menu-button sf-widget-content sf-alerts")))
            {
                foreach (var a in alertList)
                {
                    using (content.Surround(new HtmlTag("li").Class("sf-alert")))
                    {
                        content.AddLine(new HtmlTag("a")
                            .Class("sf-alert-view")
                            .Attr("onclick", new JsFunction(AlertClient.Module, "exploreAlerts", prefix, a.Property).ToString())
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
                       .Attr("onclick", new JsFunction(AlertClient.Module, "createAlert", prefix, OperationDN.UniqueKey(AlertOperation.CreateFromEntity)).ToString())
                       .InnerHtml(AlertMessage.CreateAlert.NiceToString().EncodeHtml())
                       .ToHtml());
                }
            }

            HtmlStringBuilder label = new HtmlStringBuilder();
            var toggler = new HtmlTag("a")
                .Class("sf-widget-toggler sf-alerts-toggler")
                .Attr("title", AlertMessage.Alerts.NiceToString());
            using (label.Surround(toggler))
            {
                label.Add(new HtmlTag("span")
                    .Class("ui-icon ui-icon-calendar")
                    .InnerHtml(NoteMessage.Notes.NiceToString().EncodeHtml())
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
    }
}
