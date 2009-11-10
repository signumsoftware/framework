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

namespace Signum.Web
{
    public delegate List<AlertItem> GetAlertsDelegate(HtmlHelper helper, object entity, string partialViewName, string prefix);

    public static class AlertWidgetHelper
    {
        public static Func<IdentifiableEntity, IAlertDN> CreateAlert { get; set; }
        public static Func<IdentifiableEntity, List<Lite<IAlertDN>>> RetrieveAlerts { get; set; }
        //public static Action<IdentifiableEntity, AlertsWidget> WarnedAlerts { get; set; }
        //public static Action<IdentifiableEntity, AlertsWidget> CheckedAlerts { get; set; }
        //public static Action<IdentifiableEntity, AlertsWidget> FutureAlerts { get; set; }
        public static Func<IdentifiableEntity, CountAlerts> CountAlerts { get; set; }

        public static void Start()
        {
            WidgetsHelper.GetWidgetsForView += (helper, entity, partialViewName, prefix) => WidgetsHelper_GetWidgetsForView(helper, entity, partialViewName, prefix);
        }

        private static WidgetNode WidgetsHelper_GetWidgetsForView(HtmlHelper helper, object entity, string partialViewName, string prefix)
        {
            IIdentifiable identifiable = entity as IIdentifiable;
            if (identifiable == null || identifiable.IsNew || identifiable is IAlertDN)
                return null;

            List<Lite<IAlertDN>> alerts = RetrieveAlerts((IdentifiableEntity)identifiable);
            CountAlerts count = CountAlerts((IdentifiableEntity)identifiable);

            int total = count.FutureAlerts + count.WarnedAlerts + count.CheckedAlerts;
            return new WidgetNode
            {
                Content = AlertsToString(helper, alerts, prefix),
                Count = total != 0 ? count.WarnedAlerts + " / " + total : "0",
                Label = "Alertas",
                Href = "javascript:alert('Voy');",
                Id = "Alertas",
                Show = true
            };
        }

        private static string AlertsToString(HtmlHelper helper, List<Lite<IAlertDN>> alerts, string prefix)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class='widget alerts'>");
            sb.Append("<div><i>Crear una alerta nueva...</i></div>");
            foreach (Lite<IAlertDN> alert in alerts)
            {
                //sb.AppendLine(new NoteItem{ note.ToString(helper, prefix));
            }
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}
