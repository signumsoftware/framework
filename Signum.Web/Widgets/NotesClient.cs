using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities;

namespace Signum.Web.Widgets
{
    public class NotesClient
    {
        public static string ViewPrefix = "~/signum/Views/Widgets/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<NoteDN>(EntityType.Default) { PartialViewName = _ => ViewPrefix.Formato("Note"), IsCreable = _ => false });
            }

            WidgetsHelper.GetWidgetsForView += 
                (helper, entity, partialViewName, prefix) => entity is IdentifiableEntity ? NoteWidgetHelper.CreateWidget(helper, (IdentifiableEntity)entity, partialViewName, prefix) : null;
            
            NoteWidgetHelper.CreateNote = ei => ei.IsNew ? null : new NoteDN { Target = ei.ToLite() };
            NoteWidgetHelper.Type = typeof(NoteDN);
            NoteWidgetHelper.NotesQueryColumn = "Target";
        }
    }
}