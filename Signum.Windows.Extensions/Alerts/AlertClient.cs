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
        public static void Start(params Type[] types)
        {
            Navigator.AddSetting(new EntitySettings<AlertDN>
            {
                View = e => new Alert(),
                IsCreable = EntityWhen.Never,
                Icon = ExtensionsImageLoader.GetImageSortName("alert.png"),
            });

            Navigator.AddSetting(new EntitySettings<AlertTypeDN> { View = e => new AlertType() });


            WidgetPanel.GetWidgets += (obj, mainControl) =>
             (obj is IdentifiableEntity && types.Contains(obj.GetType()) && !((IdentifiableEntity)obj).IsNew) &&
             Navigator.IsFindable(typeof(AlertDN)) ? new AlertsWidget() : null;
        }
    }
}
