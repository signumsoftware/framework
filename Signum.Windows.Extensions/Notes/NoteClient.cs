using Signum.Entities;
using Signum.Entities.Notes;
using Signum.Windows.Notes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media.Imaging;

namespace Signum.Windows.Notes
{
    public static class NoteClient
    {
        public static void Start()
        {
            if(Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                WidgetPanel.GetWidgets += (obj, mainControl) =>
                {
                    if (obj is IdentifiableEntity && !(obj is NoteDN || ((IdentifiableEntity)obj).IsNew) && Navigator.IsFindable(typeof(NoteDN)))
                        return new NotesWidget();

                    return null;
                };

                Navigator.AddSetting(new EntitySettings<NoteDN>(EntityType.Main)
                {
                    View = e => new Note(),
                    IsCreable = EntityWhen.Never,
                    Icon = BitmapFrame.Create(PackUriHelper.Reference("/Images/note.png", typeof(NotesWidget)))
                });
            }
        }
    }
}
