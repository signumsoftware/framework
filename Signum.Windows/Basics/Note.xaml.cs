using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Services;
using Signum.Entities.Basics;
using Signum.Entities;

namespace Signum.Windows.Basics
{
	public partial class Note
	{
        public Note()
		{
			this.InitializeComponent();
		}

        public static void Start()
        {
            WidgetPanel.GetWidgets += (obj, mainControl) => obj is IdentifiableEntity && !(obj is INoteDN || ((IdentifiableEntity)obj).IsNew) ? new NotesWidget() : null;

            NotesWidget.CreateNote = ei => ei.IsNew ? null : new NoteDN { Entity = ei.ToLite() };
            NotesWidget.RetrieveNotes = ei => ei == null ? null : Server.Return((INotesServer s) => s.RetrieveNotes(ei.ToLite()));

            Navigator.Manager.Settings.Add(typeof(Note), new EntitySettings { View = e => new Note(), IsCreable = admin => false, Icon = BitmapFrame.Create(PackUriHelper.Reference("/Images/note.png", typeof(NotesWidget))) });
        }
	}
}