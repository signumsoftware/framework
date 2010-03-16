using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
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
        public PartialViewResult CreateNote(string sfRuntimeTypeRelated, int? sfIdRelated, string sfOnOk, string sfOnCancel, string prefix, string sfUrl)
        {
            IdentifiableEntity entity = Database.Retrieve(Navigator.NamesToTypes[sfRuntimeTypeRelated], sfIdRelated.Value);
            ViewData[ViewDataKeys.WriteSFInfo] = true;
            return Navigator.PopupView(this, NoteWidgetHelper.CreateNote(entity), prefix, sfUrl);
        }

        public string RefreshNotes(string sfRuntimeTypeRelated, int? sfIdRelated)
        {
            IdentifiableEntity entity = Database.Retrieve(Navigator.NamesToTypes[sfRuntimeTypeRelated], sfIdRelated.Value);

            ViewDataDictionary vdd = new ViewDataDictionary();
            vdd.Add("WidgetNode", NoteWidgetHelper.CreateWidget(entity));
            HtmlHelper helper = SignumController.CreateHtmlHelper(this);
            return RenderPartialExtenders.RenderPartialToString(helper, "Views/Shared/WidgetView", vdd);
        }
        #endregion

        #region Alerts
        public PartialViewResult CreateAlert(string sfRuntimeTypeRelated, int? sfIdRelated, string sfOnOk, string sfOnCancel, string prefix, string sfUrl)
        {
            IdentifiableEntity entity = Database.Retrieve(Navigator.NamesToTypes[sfRuntimeTypeRelated], sfIdRelated.Value);
            return Navigator.PopupView(this, AlertWidgetHelper.CreateAlert(entity), prefix, sfUrl);
        }

        public string RefreshAlerts(string sfRuntimeTypeRelated, int? sfIdRelated)
        {
            IdentifiableEntity entity = Database.Retrieve(Navigator.NamesToTypes[sfRuntimeTypeRelated], sfIdRelated.Value);

            ViewDataDictionary vdd = new ViewDataDictionary();
            vdd.Add("WidgetNode", AlertWidgetHelper.CreateWidget(entity));
            HtmlHelper helper = SignumController.CreateHtmlHelper(this);
            return RenderPartialExtenders.RenderPartialToString(helper, "Views/Shared/WidgetView", vdd);
        }
        #endregion
    }
}
