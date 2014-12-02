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
                    if (obj is Entity && types.Contains(obj.GetType()) && !((Entity)obj).IsNew && Finder.IsFindable(typeof(NoteEntity)))
                        return new NotesWidget();

                    return null;
                };

                Server.SetSemiSymbolIds<NoteTypeEntity>();

                OperationClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings<Entity>(NoteOperation.CreateNoteFromEntity)  { IsVisible  = _ => false }
                });

                Navigator.AddSetting(new EntitySettings<NoteTypeEntity> { View = e => new NoteType() });
                Navigator.AddSetting(new EntitySettings<NoteEntity>
                {
                    View = e => new Note(),
                    IsCreable = EntityWhen.Never,
                    Icon = ExtensionsImageLoader.GetImageSortName("note2.png")
                });
            }
        }
    }
}
