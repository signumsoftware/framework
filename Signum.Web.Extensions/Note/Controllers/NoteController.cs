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

namespace Signum.Web.Notes
{
    public class NoteController : Controller
    {
        public ContentResult NotesCount()
        {
            var entity = (IdentifiableEntity)this.UntypedExtractEntity(); //Related entity always sent with no prefix
            int count = NoteWidgetHelper.CountNotes(entity);
            return Content(count.ToString());
        }
    }
}
