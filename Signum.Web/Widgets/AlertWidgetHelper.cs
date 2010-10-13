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
                ControllerUrl = "Widgets/CreateAlert",
                OnOkClosed = new JsFunction(){ "RefreshAlerts('Widgets/RefreshAlerts')"}
            };

            return new WidgetItem
            {
                Content =
@"<div class='widget alerts'>
    <ul>{0}</ul>{3}
    <a class='create' onclick=""javascript:RelatedEntityCreate({1});"">{2}</a>
</div>".Formato(list.Where(a => a.Count > 0).ToString(a => "<li><a href=\"javascript:OpenFinder({0});\">{1}<span class='count'>{2}</span></a></li>".Formato(JsFindOptions(identifiable, a.Query).ToJS(), a.Title, a.Count), ""),
                voptions.ToJS(),
                Properties.Resources.CreateAlert,
                list.Where(a => a.Count > 0).ToList().Count > 0 ? "<hr/>" : ""),
                Label = "<a id='{0}'>{0}{1}</a>".Formato(
                    Properties.Resources.Alerts,
                    list.ToString(a => "<span class='count {0} {1}'>{2}</span>".Formato(a.Class, a.Count == 0 ? "disabled" : "", a.Count), "")),
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
