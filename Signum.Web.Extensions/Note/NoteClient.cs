using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities;
using Signum.Entities.Notes;
using Signum.Web.Operations;

namespace Signum.Web.Notes
{
    public static class NoteClient
    {
        public static string ViewPrefix = "~/Note/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Note/Scripts/Notes");
        public static Type[] Types; 

        public static void Start(params Type[] types)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (types == null)
                    throw new ArgumentNullException("types");

                Navigator.RegisterArea(typeof(NoteClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<NoteTypeDN>{ PartialViewName = _ => ViewPrefix.Formato("NoteType") },
                    new EntitySettings<NoteDN> 
                    { 
                        PartialViewName = _ => ViewPrefix.Formato("Note"), 
                        IsCreable = EntityWhen.Never 
                    },
                });

                Types = types;

                WidgetsHelper.GetWidget += WidgetsHelper_GetWidget;
                OperationClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings<IdentifiableEntity>(NoteOperation.CreateNoteFromEntity) 
                    { 
                        IsVisible  = _ => false
                    }
                });
            }
        }

        static IWidget WidgetsHelper_GetWidget(WidgetContext ctx)
        {
            IdentifiableEntity ie = ctx.Entity as IdentifiableEntity;
            if (ie == null || ie.IsNew)
                return null;

            if (!Types.Contains(ie.GetType()))
                return null;

            if (!Finder.IsFindable(typeof(NoteDN)))
                return null;

            return NoteWidgetHelper.CreateWidget(ctx);
        }
    }
}