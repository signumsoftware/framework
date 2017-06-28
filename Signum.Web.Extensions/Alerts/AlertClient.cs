using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities;
using Signum.Entities.Alerts;
using Signum.Web.Operations;

namespace Signum.Web.Alerts
{
    public static class AlertClient
    {
        public static string ViewPrefix = "~/Alerts/Views/{0}.cshtml";

        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Alerts/Scripts/Alerts");

        public static Type[] Types;

        public static void Start(params Type[] types)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (types == null)
                    throw new ArgumentNullException("types");

                Navigator.RegisterArea(typeof(AlertClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<AlertEntity> { PartialViewName = _ => ViewPrefix.FormatWith("Alert") },
                    new EntitySettings<AlertTypeEntity> { PartialViewName = _ => ViewPrefix.FormatWith("AlertType") },
                });

                Types = types;

                WidgetsHelper.GetWidget += WidgetsHelper_GetWidget;

                OperationClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings<Entity>(AlertOperation.CreateAlertFromEntity){ IsVisible = a => false },
                    //new EntityOperationSettings<AlertEntity>(AlertOperation.SaveNew){ IsVisible = a => a.Entity.IsNew },
                    new EntityOperationSettings<AlertEntity>(AlertOperation.Save){ IsVisible = a => !a.Entity.IsNew }
                });
            }
        }

        public static IWidget WidgetsHelper_GetWidget(WidgetContext ctx)
        {
            Entity ie = ctx.Entity as Entity;
            if (ie == null || ie.IsNew)
                return null;

            if (!Types.Contains(ie.GetType()))
                return null;

            if (!Finder.IsFindable(typeof(AlertEntity)))
                return null;

            return AlertWidgetHelper.CreateWidget(ctx);
        }
    }
}