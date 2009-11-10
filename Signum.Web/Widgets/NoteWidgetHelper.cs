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

            FindO
            return new WidgetNode{
                Content = NotesToString(helper, notes, prefix),
                Count = notes.Count.ToString(),
                Label = "Notes",
                Href = "javascript:Find('Signum.aspx/PartialFind', 'NotaDN', false, function(){OnSearchOk('widgetNote', 'divASustituir');}, function(){OnSearchCancel('widgetNote', 'divASustituir');} , 'divASustituir', 'widgetNote');",
                Id = "Notes",
                Show = true
            };
        }

        private static string NotesToString(HtmlHelper helper, List<Lite<INoteDN>> notes, string prefix)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class='widget notes'>");
            
            string jscript=
                  @"new popup('Signum.aspx/PopupView').typedDirect('NotaDN')";

            sb.Append("<div><a class='create' onclick=\"{0}\">Crear una nota nueva...</a></div>"
                .Formato(jscript));
            
            if (notes.Count > 0) sb.Append("<hr />");
            foreach (Lite<INoteDN> note in notes)
            {
                string jscript2 = Navigator.ViewRoute(typeof(NoteDN), note.Id);
                    //@"new popup('Signum.aspx/PopupView').typed('NotaDN', '{0}','','');".Formato("sfId=" + note.Id);

                sb.Append("<div><a class='navigate' onclick=\"{0}\">{1}</a></div>"
              .Formato(jscript2, note.ToStr.Etc(30)
              ));
            }
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}
