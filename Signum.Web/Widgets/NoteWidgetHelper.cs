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
               /* return "javascript:QuickLinkClickServerAjax('{0}','{1}','{2}');".Formato(
                controllerUrl,

                FindOptions.ToString(true, ""),
                prefix
                );*/
            return "";
            
            /*return "javascript:QuickLinkClickServerAjax('{0}','{1}','{2}');".Formato(
                controllerUrl,
                "divASustituir",
                ""
                "NotaDN",   //TODO: Cambiar!
                "javascript:UpdateNoteCount();");*/
        }
        //function OpenPopup(urlController, divASustituir, prefix, onOk, onCancel, detailDiv, partialView) {


        private static string NotesToString(HtmlHelper helper, List<Lite<INoteDN>> notes, string prefix)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class='widgetDiv notesDiv'>");
            sb.Append("<div><i>Crear una nota nueva...</i></div>");
            if (notes.Count > 0) sb.Append("<hr />");
            foreach (Lite<INoteDN> note in notes)
            {
                sb.AppendLine("<div>" + note.ToStr.Etc(30) + "</div>");
                //sb.AppendLine(new NoteItem{ note.ToString(helper, prefix));
            }
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}
