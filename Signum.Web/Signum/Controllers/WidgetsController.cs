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

namespace Signum.Web.Controllers
{
    public class WidgetsController : Controller
    {
        #region Notes
        public PartialViewResult CreateNote(string prefix)
        {
            var entity = (IdentifiableEntity)this.UntypedExtractEntity(); //Related entity always sent with no prefix
            
            TypeContext tc = TypeContextUtilities.UntypedNew(NoteWidgetHelper.CreateNote(entity), prefix);
            return this.PopupOpen(new PopupNavigateOptions(tc));
        }

        public ContentResult NotesCount()
        {
            var entity = (IdentifiableEntity)this.UntypedExtractEntity(); //Related entity always sent with no prefix
            int count = NoteWidgetHelper.CountNotes(entity);
            return Content(count.ToString());
        }
        #endregion

        #region Alerts
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
                warned = AlertWidgetHelper.CountAlerts(entity, AlertWidgetHelper.WarnedAlertsQuery),
                future = AlertWidgetHelper.CountAlerts(entity, AlertWidgetHelper.FutureAlertsQuery),
                attended = AlertWidgetHelper.CountAlerts(entity, AlertWidgetHelper.AttendedAlertsQuery),
            });
        }
        #endregion
    }
}
