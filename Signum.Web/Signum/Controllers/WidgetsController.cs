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
            ViewData[ViewDataKeys.WriteSFInfo] = true;
            return Navigator.PopupView(this, NoteWidgetHelper.CreateNote(entity), prefix);
        }

        public ContentResult NotesCount()
        {
            var entity = (IdentifiableEntity)this.UntypedExtractEntity(); //Related entity always sent with no prefix
            int count = NoteWidgetHelper.CountNotes(entity);
            return Content(count.ToString());
        }
        #endregion

        #region Alerts
        public PartialViewResult CreateAlert(string sfRuntimeTypeRelated, int? sfIdRelated, string prefix, string url)
        {
            IdentifiableEntity entity = Database.Retrieve(Navigator.ResolveType(sfRuntimeTypeRelated), sfIdRelated.Value);
            ViewData[ViewDataKeys.WriteSFInfo] = true;
            return Navigator.PopupView(this, AlertWidgetHelper.CreateAlert(entity), prefix, url);
        }

        public MvcHtmlString RefreshAlerts(string sfRuntimeTypeRelated, int? sfIdRelated)
        {
            IdentifiableEntity entity = Database.Retrieve(Navigator.ResolveType(sfRuntimeTypeRelated), sfIdRelated.Value);

            ViewDataDictionary vdd = new ViewDataDictionary();
            vdd.Add("WidgetNode", AlertWidgetHelper.CreateWidget(entity));
            HtmlHelper helper = SignumController.CreateHtmlHelper(this);
            return helper.Partial("WidgetView", vdd);
        }
        #endregion
    }
}
