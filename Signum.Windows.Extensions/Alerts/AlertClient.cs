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

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<AlertTypeDN> { View = e => new AlertType() },
                    new EntitySettings<AlertDN>
                    {
                        View = e => new Alert(),
                        IsCreable = EntityWhen.Never,
                        Icon = ExtensionsImageLoader.GetImageSortName("alert.png"),
                    }   
                });

                OperationClient.AddSettings(new List<OperationSettings> 
                {
                    new EntityOperationSettings(AlertOperation.CreateFromEntity){ IsVisible = a => false },
                    new EntityOperationSettings(AlertOperation.SaveNew){ IsVisible = a => a.Entity.IsNew },
                    new EntityOperationSettings(AlertOperation.Save){ IsVisible = a => !a.Entity.IsNew }
                });

                WidgetPanel.GetWidgets += (obj, mainControl) =>
                 (obj is IdentifiableEntity && types.Contains(obj.GetType()) && !((IdentifiableEntity)obj).IsNew) &&
                 Navigator.IsFindable(typeof(AlertDN)) ? new AlertsWidget() : null;
            }
        }
    }
}
