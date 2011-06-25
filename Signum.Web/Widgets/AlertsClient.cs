using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities;

namespace Signum.Web.Widgets
{
    public class AlertsClient
    {
        public static string ViewPrefix = "~/signum/Views/Widgets/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<AlertDN>(EntityType.Default) { PartialViewName = _ => ViewPrefix.Formato("Alert"), IsCreable = _ => false });
            }

            WidgetsHelper.GetWidgetsForView += 
                (helper, entity, partialViewName, prefix) => entity is IdentifiableEntity ? AlertWidgetHelper.CreateWidget(helper, (IdentifiableEntity)entity, partialViewName, prefix) : null;

            AlertWidgetHelper.AlertType = typeof(AlertDN);
            AlertWidgetHelper.CreateAlert = ei => ei.IsNew ? null : new AlertDN { Entity = ei.ToLite() };
            AlertWidgetHelper.AlertsQueryColumn = "Target";
            AlertWidgetHelper.WarnedAlertsQuery = AlertQueries.NotAttended;
            AlertWidgetHelper.FutureAlertsQuery = AlertQueries.Future;
            AlertWidgetHelper.AttendedAlertsQuery = AlertQueries.Attended;
        }
    }
}