using Signum.Entities;
using Signum.Entities.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Windows.Alerts
{
    public static class AlertClient
    {
        public static void Start(params Type[] tipos)
        {
            Navigator.AddSetting(new EntitySettings<AlertDN>(EntityType.Main)
            {
                View = e => new Alert(),
                IsCreable = EntityWhen.Never,
                Icon = ExtensionsImageLoader.GetImageSortName("alert.png"),
            });

            Navigator.AddSetting(new EntitySettings<AlertTypeDN>(EntityType.String) { View = e => new AlertType() });


            WidgetPanel.GetWidgets += (obj, mainControl) =>
             (obj is IdentifiableEntity && tipos.Contains(obj.GetType()) && !((IdentifiableEntity)obj).IsNew) &&
             Navigator.IsFindable(typeof(AlertDN)) ? new AlertsWidget() : null;
        }
    }
}
