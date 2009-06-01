using System;
using System.Collections.Generic;
using System.Linq;
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
using Signum.Entities.Basics;
using Signum.Entities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for NotesWidget.xaml
    /// </summary>
    public partial class NotesWidget : UserControl, IWidget
    {
        public event Action ForceShow;

        public static Func<IdentifiableEntity, INoteDN> CreateNote { get; set; }
        public static Func<IdentifiableEntity, List<Lazy<INoteDN>>> RetrieveNotes { get; set; }

        public NotesWidget()
        {
            InitializeComponent();

            lvNotas.AddHandler(Button.ClickEvent, new RoutedEventHandler(Note_MouseDown));
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(NotesWidget_DataContextChanged);
        }

        void NotesWidget_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ReloadNotes(); 
        }

        private void Note_MouseDown(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button) //Not to capture the mouseDown of the scrollbar buttons
            {
                Button b = (Button)e.OriginalSource;
                Lazy<INoteDN> nota = (Lazy<INoteDN>)b.Tag;
                ViewNote(Server.RetrieveLazyThin(nota));
            }
        }

        private void btnNewNote_Click(object sender, RoutedEventArgs e)
        {
            if (CreateNote == null)
                throw new ApplicationException("NotesWidget.CreateNote is null");

            if (DataContext == null)
                return;

            INoteDN nota = CreateNote((IdentifiableEntity)DataContext);

            ViewNote(nota);
        }

        void ViewNote(INoteDN note)
        {
            INoteDN result = (INoteDN)Navigator.View(new ViewOptions { Buttons = ViewButtons.Save, Modal = true }, note);

            ReloadNotes();
        }

        private void ReloadNotes()
        {
            if (CreateNote == null)
                throw new ApplicationException("NotesWidget.RetrieveNotes is null"); 

            if (DataContext == null)
            {
                lvNotas.ItemsSource = null;
                return; 
            }

            List<Lazy<INoteDN>> notes = RetrieveNotes((IdentifiableEntity)DataContext);

            if (notes != null)
            {
                tbNotes.FontWeight = notes.Count == 0 ? FontWeights.Normal : FontWeights.Bold;

                if (notes.Count > 0 && ForceShow != null)
                    ForceShow();
            }

            lvNotas.ItemsSource = notes;
        }
    }
}
