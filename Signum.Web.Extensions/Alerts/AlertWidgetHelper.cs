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
        public static AlertEntity CreateAlert(Entity entity)
        {
            if(entity.IsNew)
                return null;

            return new AlertEntity { Target = entity.ToLite() };
        }

        public static int CountAlerts(Lite<Entity> identifiable, string filterField)
        {
            return Finder.QueryCount(new CountOptions(typeof(AlertEntity))
            {
                FilterOptions = 
                {
                    new FilterOption("Target", identifiable),
                    new FilterOption("Entity." + filterField, true) 
                },
            });
        }

        public static Widget CreateWidget(WidgetContext ctx)
        {
            var ident = (Entity)ctx.Entity;

            var url = RouteHelper.New().Action((AlertController ac) => ac.AlertsCount());

            var alertList = new[]
            {
                new { Count = CountAlerts(ident.ToLite(), "Attended"), Property = "Attended", AlertClass = "sf-alert-attended", Title = AlertMessage.Alerts_Attended.NiceToString() },
                new { Count = CountAlerts(ident.ToLite(), "Alerted"), Property = "Alerted", AlertClass = "sf-alert-alerted", Title = AlertMessage.Alerts_NotAttended.NiceToString() },
                new { Count = CountAlerts(ident.ToLite(), "Future"), Property = "Future", AlertClass = "sf-alert-future", Title = AlertMessage.Alerts_Future.NiceToString() },
            };

            var items = alertList.Select(a => new MenuItem(ctx.Prefix, "sfAlertExplore_" + a.Property)
            {
                OnClick = AlertClient.Module["exploreAlerts"](ctx.Prefix, GetFindOptions(ident, a.Property).ToJS(ctx.Prefix, "alerts"), url),
                CssClass = "sf-alert-view",
                Html = 
                new HtmlTag("span").Class("sf-alert-count-label").Class(a.AlertClass).Class(a.Count > 0 ? "sf-alert-active" : null).InnerHtml((a.Title + ": ").EncodeHtml()).ToHtml().Concat(
                new HtmlTag("span").Class(a.AlertClass).Class(a.Count > 0 ? "sf-alert-active" : null).SetInnerText(a.Count.ToString()))
            }).Cast<IMenuItem>().ToList();

            items.Add(new MenuItemSeparator());

            items.Add(new MenuItem(ctx.Prefix, "sfAlertCreate")
            {
                CssClass = "sf-alert-create",
                OnClick = AlertClient.Module["createAlert"](JsFunction.Event, ctx.Prefix, AlertOperation.CreateAlertFromEntity.Symbol.Key, url),
                Text = AlertMessage.CreateAlert.NiceToString(),
            }); 

            HtmlStringBuilder label = new HtmlStringBuilder();
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

            return new Widget
            {
                Title = AlertMessage.Alerts.NiceToString(),
                IconClass = "glyphicon glyphicon-bell",
                Class = "sf-alerts-toggler",
                Id = TypeContextUtilities.Compose(ctx.Prefix, "alertsWidget"),
                Active = alertList.Any(a => a.Count > 0),
                Html = label.ToHtml(),
                Items = items,
            };
        }

        private static FindOptions GetFindOptions(Entity ident, string property)
        {
            return new FindOptions
            {
                QueryName = typeof(AlertEntity),
                Create = false,
                SearchOnLoad = true,
                ShowFilters = false,
                FilterOptions = 
                { 
                    new FilterOption("Target", ident.ToLite()),
                    new FilterOption("Entity." + property, true),
                },
                ColumnOptions = { new ColumnOption("Target") },
                ColumnOptionsMode = ColumnOptionsMode.Remove,
            };
        }
    }
}
