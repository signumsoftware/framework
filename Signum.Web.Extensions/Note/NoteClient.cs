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
        public static string Module = "Extensions/Signum.Web.Extensions/Notes/Scripts/Notes";

        public static void Start(params Type[] types)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
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

                WidgetsHelper.GetWidgetsForView += (entity, partialViewName, prefix) =>
                    SupportsNotes(entity, types) ? NoteWidgetHelper.CreateWidget(entity as IdentifiableEntity, partialViewName, prefix) :
                    null;

                OperationClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings(NoteOperation.CreateFromEntity) 
                    { 
                        IsVisible  = _ => false
                    }
                });
            }
        }

        static bool SupportsNotes(ModifiableEntity entity, Type[] tipos)
        {
            IdentifiableEntity ie = entity as IdentifiableEntity;
            if (ie == null || ie.IsNew)
                return false;

            if (!tipos.Contains(ie.GetType()))
                return false;

            return Navigator.IsFindable(typeof(NoteDN));
        }
    }
}