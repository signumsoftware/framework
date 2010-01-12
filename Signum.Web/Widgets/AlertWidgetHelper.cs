using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Engine;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;

namespace Signum.Web
{
    public delegate List<AlertItem> GetAlertsDelegate(HtmlHelper helper, object entity, string partialViewName, string prefix);

    public static class AlertWidgetHelper
    {
        public static Func<IdentifiableEntity, IAlertDN> CreateAlert { get; set; }
        public static object WarnedAlertsQuery { get; set; }
        public static object CheckedAlertsQuery { get; set; }
        public static object FutureAlertsQuery { get; set; }
        public static string AlertsQueryColumn { get; set; }

        public static WidgetItem CreateWidget(IdentifiableEntity identifiable)
        {
            if (identifiable == null || identifiable.IsNew || identifiable is IAlertDN)
                return null;

            var list = new []
            {
                new { Count = GetCount(WarnedAlertsQuery, identifiable), Query = WarnedAlertsQuery, Class = "warned", Title = Properties.Resources.Warned },
                new { Count = GetCount(CheckedAlertsQuery, identifiable), Query = CheckedAlertsQuery, Class = "checked", Title = Properties.Resources.Checked },
                new { Count = GetCount(FutureAlertsQuery, identifiable), Query = FutureAlertsQuery, Class = "future", Title = Properties.Resources.Future },
            };

            JsViewOptions voptions = new JsViewOptions
            {
                Type = null, //Navigator.TypesToURLNames[typeof(AlertDN)],
                ControllerUrl = "Widgets/CreateAlert",
                OnOkSuccess = "function(){ RefreshAlerts('Widgets/RefreshAlerts','AlertDN'); }"
            };

            return new WidgetItem
            {
                Content = 
@"<div class='widget alerts'>
    <ul>{0}</ul>
    <a class='create' onclick=""javascript:RelatedEntityCreate({1});"">{2}</a>
</div>".Formato(list.Where(a => a.Count > 0).ToString(a => "<li><a href='javascript:OpenFinder(\"{0}\");'>{1}</a></li>".Formato(JsFindOptions(identifiable, a.Query), a.Title), ""),
                voptions.ToJS(), 
                Properties.Resources.CreateNewAlert),

                Label = "<a id='Alerts'>{0}{1}</a>".Formato(
                    Properties.Resources.Alerts,
                    list.ToString(a => "<span class='{0} {1}'>{2}</span>".Formato(a.Class, a.Count == 0 ? "disabled" : "", a.Count), "")),
                Id = "Alerts",
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
                    FilterMode = FilterMode.Hidden,
                    FilterOptions = new List<FilterOptions>
                    {
                        new FilterOptions(AlertsQueryColumn,identifiable.ToLite())
                    }
                }
            };
            return foptions;
        }

        private static int GetCount(object queryName, IdentifiableEntity identifiable)
        {
            int count = Navigator.QueryCount(new QueryOptions(queryName)
            {
                FilterOptions = new List<FilterOptions>
                {
                    new FilterOptions(AlertsQueryColumn, identifiable) 
                }
            });
            return count;
        }
    }
}
