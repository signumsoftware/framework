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
        public static string ViewPrefix = "~/Alert/Views/{0}.cshtml";

        public static string Module = "Extensions/Signum.Web.Extensions/Alerts/Scripts/Alerts";

        public static void Start(params Type[] types)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(AlertClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<AlertDN> { PartialViewName = _ => ViewPrefix.Formato("Alert") },
                    new EntitySettings<AlertTypeDN> { PartialViewName = _ => ViewPrefix.Formato("AlertType") },
                });

                WidgetsHelper.GetWidgetsForView += (entity, partialViewName, prefix) =>
                    SupportAlerts(entity, types) ? AlertWidgetHelper.CreateWidget(entity as IdentifiableEntity, partialViewName, prefix) :
                    null;

                OperationClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings(AlertOperation.CreateFromEntity){ IsVisible = a => false },
                    new EntityOperationSettings(AlertOperation.SaveNew){ IsVisible = a => a.Entity.IsNew },
                    new EntityOperationSettings(AlertOperation.Save){ IsVisible = a => !a.Entity.IsNew }
                });
            }
        }

        static bool SupportAlerts(ModifiableEntity entity, params Type[] tipos)
        {
            IdentifiableEntity ie = entity as IdentifiableEntity;
            if (ie == null || ie.IsNew)
                return false;

            if (!tipos.Contains(ie.GetType()))
                return false;

            return Navigator.IsFindable(typeof(AlertDN));
        }
    }
}