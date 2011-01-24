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
    [HandleException, AuthenticationRequired]
    public class WidgetsController : Controller
    {
        #region Notes
        public PartialViewResult CreateNote(string sfRuntimeTypeRelated, int? sfIdRelated, string prefix, string url)
        {
            IdentifiableEntity entity = Database.Retrieve(Navigator.ResolveType(sfRuntimeTypeRelated), sfIdRelated.Value);
            ViewData[ViewDataKeys.WriteSFInfo] = true;
            return Navigator.PopupView(this, NoteWidgetHelper.CreateNote(entity), prefix, url);
        }

        public MvcHtmlString RefreshNotes(string sfRuntimeTypeRelated, int? sfIdRelated)
        {
            IdentifiableEntity entity = Database.Retrieve(Navigator.ResolveType(sfRuntimeTypeRelated), sfIdRelated.Value);

            ViewDataDictionary vdd = new ViewDataDictionary();
            vdd.Add("WidgetNode", NoteWidgetHelper.CreateWidget(entity));
            HtmlHelper helper = SignumController.CreateHtmlHelper(this);
            return helper.Partial("Views/Shared/WidgetView", vdd);
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
            return helper.Partial("Views/Shared/WidgetView", vdd);
        }
        #endregion
    }
}
