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
    public class AlertsController : Controller
    {
        public PartialViewResult CreateAlert(string prefix)
        {
            var entity = (IdentifiableEntity)this.UntypedExtractEntity(); //Related entity always sent with no prefix

            TypeContext tc = TypeContextUtilities.UntypedNew(AlertWidgetHelper.CreateAlert(entity), prefix);
            return this.PopupOpen(new PopupNavigateOptions(tc));
        }

        public JsonResult AlertsCount()
        {
            var entity = (IdentifiableEntity)this.UntypedExtractEntity(); //Related entity always sent with no prefix
            return Json(new
            {
                warned = AlertWidgetHelper.CountAlerts(entity, "NotAttended"),
                future = AlertWidgetHelper.CountAlerts(entity, "Future"),
                attended = AlertWidgetHelper.CountAlerts(entity, "Attended"),
            });
        }
    }
}
