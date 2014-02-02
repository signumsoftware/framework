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
        public JsonResult AlertsCount()
        {
            var entity = (IdentifiableEntity)this.UntypedExtractEntity(); //Related entity always sent with no prefix
            return Json(new
            {
                Alerted = AlertWidgetHelper.CountAlerts(entity, "Alerted"),
                Future = AlertWidgetHelper.CountAlerts(entity, "Future"),
                Attended = AlertWidgetHelper.CountAlerts(entity, "Attended"),
            });
        }
    }
}
