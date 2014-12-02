using Signum.Entities;
using Signum.Entities.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Operations;
using System.Reflection;

namespace Signum.Windows.Alerts
{
    public static class AlertClient
    {
        public static void Start(params Type[] types)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (types == null)
                    throw new ArgumentNullException("types");

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<AlertTypeEntity> { View = e => new AlertType() },
                    new EntitySettings<AlertEntity>
                    {
                        View = e => new Alert(),
                        IsCreable = EntityWhen.Never,
                        Icon = ExtensionsImageLoader.GetImageSortName("alert.png"),
                    }   
                });

                Server.SetSemiSymbolIds<AlertTypeEntity>();

                OperationClient.AddSettings(new List<OperationSettings> 
                {
                    new EntityOperationSettings<Entity>(AlertOperation.CreateAlertFromEntity){ IsVisible = a => false },
                    new EntityOperationSettings<AlertEntity>(AlertOperation.SaveNew){ IsVisible = a => a.Entity.IsNew },
                    new EntityOperationSettings<AlertEntity>(AlertOperation.Save){ IsVisible = a => !a.Entity.IsNew }
                });

                WidgetPanel.GetWidgets += (obj, mainControl) =>
                    (obj is Entity && types.Contains(obj.GetType()) && !((Entity)obj).IsNew) &&
                    Finder.IsFindable(typeof(AlertEntity)) ? new AlertsWidget() : null;
            }
        }
    }
}
