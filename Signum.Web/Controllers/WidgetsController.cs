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
    [HandleError]
    public class WidgetsController : Controller
    {
        public PartialViewResult CreateNote(string sfRuntimeTypeRelated, int? sfIdRelated, string sfOnOk, string sfOnCancel, string prefix, string sfUrl)
        {
            IdentifiableEntity entity = Database.Retrieve(Navigator.NameToType[sfRuntimeTypeRelated], sfIdRelated.Value);
            return Navigator.PopupView(this, NoteWidgetHelper.CreateNote(entity), prefix, sfUrl);
        }
    }
}
