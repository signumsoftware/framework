using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Basics;

namespace Signum.Web
{
    //public delegate List<NoteItem> GetNotesDelegate(HtmlHelper helper, object entity, string partialViewName, string prefix);

    public static class NoteWidgetHelper
    {
        //public static event GetNotesDelegate GetNotes;
        public static Func<IdentifiableEntity, INoteDN> CreateNote { get; set; }
        public static Func<IdentifiableEntity, List<Lite<INoteDN>>> RetrieveNotes { get; set; }
        public static Func<List<Lite<INoteDN>>,IIdentifiable, WidgetNode> RetrieveNode { get; set; }
        public static void Start()
        {
            WidgetsHelper.GetWidgetsForView += (helper, entity, partialViewName, prefix) => WidgetsHelper_GetWidgetsForView(helper, entity, partialViewName, prefix);
        }

        private static WidgetNode WidgetsHelper_GetWidgetsForView(HtmlHelper helper, object entity, string partialViewName, string prefix)
        {
            IIdentifiable identifiable = entity as IIdentifiable;
            if (identifiable == null || identifiable.IsNew || identifiable is INoteDN)
                return null;

            List<Lite<INoteDN>> notes = RetrieveNotes((IdentifiableEntity)identifiable);
            return RetrieveNode(notes, identifiable);

        }


    }
}
