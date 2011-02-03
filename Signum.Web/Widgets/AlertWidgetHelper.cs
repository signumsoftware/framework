using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;

namespace Signum.Web
{
    //public delegate List<AlertItem> GetAlertsDelegate(HtmlHelper helper, object entity, string partialViewName, string prefix);

    public static class AlertWidgetHelper
    {
        public static Type Type { get; set; }
        public static Func<IdentifiableEntity, IAlertDN> CreateAlert { get; set; }
        public static object WarnedAlertsQuery { get; set; }
        public static object CheckedAlertsQuery { get; set; }
        public static object FutureAlertsQuery { get; set; }
        public static string AlertsQueryColumn { get; set; }

        public static WidgetItem CreateWidget(IdentifiableEntity identifiable)
        {
            if (identifiable == null || identifiable.IsNew || identifiable is IAlertDN)
                return null;

            var list = new[]
            {
                new { Count = GetCount(WarnedAlertsQuery, identifiable), Query = WarnedAlertsQuery, Class = "warned", Title = Properties.Resources.Warned },
                new { Count = GetCount(CheckedAlertsQuery, identifiable), Query = CheckedAlertsQuery, Class = "checked", Title = Properties.Resources.Checked },
                new { Count = GetCount(FutureAlertsQuery, identifiable), Query = FutureAlertsQuery, Class = "future", Title = Properties.Resources.Future },
            };

            JsViewOptions voptions = new JsViewOptions
            {
                Type = Type.Name,
                ControllerUrl = RouteHelper.New().Action("CreateAlert", "Widgets"),
                OnOkClosed = new JsFunction(){ "RefreshAlerts('{0}')".Formato(RouteHelper.New().Action("RefreshAlerts", "Widgets"))}
            };


            HtmlStringBuilder content = new HtmlStringBuilder(); 
            using(content.Surround(new HtmlTag("div").Class("widget alerts")))
            {
                using(content.Surround("ul"))
                {
                    foreach (var a in list.Where(a=>a.Count > 0))
	                {
                        content.Add(new HtmlTag("a")
                                    .Attr("href", "javascript:new SF.FindNavigator({0}).openFinder();".Formato(JsFindOptions(identifiable, a.Query).ToJS()))
                                    .InnerHtml(
                                        a.Title.EncodeHtml(), 
                                        new HtmlTag("span").Class("count").SetInnerText(a.Count.ToString())
                                    ));
		 
	                }
                }

                if(list.Count(a=>a.Count > 0) > 0)
                    content.Add(new HtmlTag("hr").ToHtmlSelf());

                content.Add(new HtmlTag("a")
                    .Class("create")
                    .Attr("onclick","javascript:SF.relatedEntityCreate({1});".Formato(voptions.ToJS()))
                    .SetInnerText(Properties.Resources.CreateAlert));
            }

            return new WidgetItem
            {
                Content = content.ToHtml(),
                
                Label = new HtmlTag("a", "Alerts").InnerHtml(
                    Properties.Resources.Alerts.EncodeHtml(),
                    new HtmlStringBuilder(
                        list.Select(a=>
                            new HtmlTag("span")
                                .Class("count")
                                .Class(a.Class)
                                .Class(a.Count == 0 ? "disabled" : "")
                                .SetInnerText(a.Count.ToString())
                                .ToHtml())
                        ).ToHtml()
                    ).ToHtml(),
                
               
                Id = Properties.Resources.Alerts,
                Show = true
            };
        }

        private static JsFindOptions JsFindOptions(IdentifiableEntity identifiable, object queryName)
        {
            JsFindOptions foptions = new JsFindOptions
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
            return foptions;
        }

        private static int GetCount(object queryName, IdentifiableEntity identifiable)
        {
            int count = Navigator.QueryCount(new CountOptions(queryName)
            {
                FilterOptions = { new FilterOption(AlertsQueryColumn, identifiable) },
            });
            return count;
        }
    }
}
