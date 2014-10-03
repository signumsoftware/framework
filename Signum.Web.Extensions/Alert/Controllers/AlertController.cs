using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Web;

namespace Signum.Web.Alerts
{
    public class AlertController : Controller
    {
        public JsonNetResult AlertsCount()
        {
            var entity = Lite.Parse<Entity>(Request["key"]);
            return this.JsonNet(new
            {
                Alerted = AlertWidgetHelper.CountAlerts(entity, "Alerted"),
                Future = AlertWidgetHelper.CountAlerts(entity, "Future"),
                Attended = AlertWidgetHelper.CountAlerts(entity, "Attended"),
            });
        }
    }
}
