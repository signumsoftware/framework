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
            
            return new WidgetNode{
                Content = NotesToString(helper, notes, prefix),
                Count = notes.Count.ToString(),
                Label = "Notas",
                Href = GetServerClickAjax(""),
                Id = "Notas",
                Show = true
            };
        }

        private static string GetServerClickAjax(string prefix)
        {
           // if (OnClick.HasText() || OnServerClickPost.HasText())
            //    return null;

            string controllerUrl = "Signum.aspx/PartialFind";
            //if (OnServerClickAjax.HasText())
            //    controllerUrl = OnServerClickAjax;

            return "javascript:OpenPopup('{0}','{1}','{2}');".Formato(
                controllerUrl,
                "divASustituir",
                "NotaDN",   //TODO: Cambiar!
                "javascript:UpdateNoteCount();");
        }

        private static string NotesToString(HtmlHelper helper, List<Lite<INoteDN>> notes, string prefix)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class='widgetDiv notesDiv'>");
            sb.Append("<div><i>Crear una nota nueva...</i></div><hr />");
            foreach (Lite<INoteDN> note in notes)
            {
                sb.AppendLine("<div onclick='{0}'>{1}</div>".Formato(
                    "javascript:OpenPopup('{0}','{1}','{2}');".Formato(
                    "Signum.aspx/PartialView",
                    "divASustituir",
                    "NotaDN",   //TODO: Cambiar!
                    "javascript:UpdateNoteCount();"),
                    note.ToStr.Etc(30)                    
                    ));
                //sb.AppendLine(new NoteItem{ note.ToString(helper, prefix));
            }
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}
