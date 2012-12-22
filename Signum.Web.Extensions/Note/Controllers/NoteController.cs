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
    }
}
