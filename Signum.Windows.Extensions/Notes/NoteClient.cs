using Signum.Entities;
using Signum.Entities.Notes;
using Signum.Windows.Notes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media.Imaging;
using Signum.Windows.Operations;

namespace Signum.Windows.Notes
{
    public static class NoteClient
    {
        public static void Start(params Type[] types)
        {
            if(Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (types == null)
                    throw new ArgumentNullException("types");

                WidgetPanel.GetWidgets += (obj, mainControl) =>
                {
                    if (obj is IdentifiableEntity && types.Contains(obj.GetType()) && !((IdentifiableEntity)obj).IsNew && Finder.IsFindable(typeof(NoteDN)))
                        return new NotesWidget();

                    return null;
                };

                Server.SetSemiSymbolIds<NoteTypeDN>();

                OperationClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings<NoteDN>(NoteOperation.CreateNoteFromEntity) 
                    { 
                        IsVisible  = _ => false
                    }
                });

                Navigator.AddSetting(new EntitySettings<NoteTypeDN> { View = e => new NoteType() });
                Navigator.AddSetting(new EntitySettings<NoteDN>
                {
                    View = e => new Note(),
                    IsCreable = EntityWhen.Never,
                    Icon = ExtensionsImageLoader.GetImageSortName("note2.png")
                });
            }
        }
    }
}
