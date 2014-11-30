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
using Signum.Services;
using Signum.Windows.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Entities.Notes;


namespace Signum.Windows.Notes
{
    /// <summary>
    /// Interaction logic for NotesWidget.xaml
    /// </summary>
    public partial class NotesWidget : UserControl, IWidget
    {
        public event Action ForceShow;

        public static NoteEntity CreateNote(Entity entity)
        {
            if (entity.IsNew)
                return null;

            return new NoteEntity
            {
                Target = entity.ToLite(),
                CreationDate = Server.Return((IBaseServer s) => s.ServerNow()),
                CreatedBy = UserEntity.Current.ToLite(),
            };
        }

        public NotesWidget()
        {
            InitializeComponent();

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
                Lite<NoteEntity> nota = (Lite<NoteEntity>)b.Tag;
                ViewNote(Server.RetrieveAndForget(nota));
            }
        }

        private void btnNewNote_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
                return;

            NoteEntity nota = CreateNote((Entity)DataContext);

            ViewNote(nota);
        }

        public static Polymorphic<Func<Entity, FilterOption>> CustomFilter = new Polymorphic<Func<Entity, FilterOption>>();

        private void btnExploreNotes_Click(object sender, RoutedEventArgs e)
        {
            var func = CustomFilter.TryGetValue(DataContext.GetType());

            var eo = new ExploreOptions(typeof(NoteEntity))
            {
                ShowFilters = false,
                SearchOnLoad = true,
                FilterOptions =
                {
                    func != null ?  func((Entity)DataContext) : new FilterOption("Target", DataContext) { Frozen = true },
                },
                OrderOptions = { new OrderOption("CreationDate", OrderType.Ascending) },
                Closed = (_, __) => Dispatcher.Invoke(() => ReloadNotes())
            };

            if (func == null)
            {
                eo.ColumnOptions = new List<ColumnOption> { new ColumnOption("Target") };
                eo.ColumnOptionsMode = ColumnOptionsMode.Remove;
            }

            Finder.Explore(eo);
        }

        void ViewNote(NoteEntity note)
        {
            Navigator.NavigateUntyped(note, new NavigateOptions()
            {
                Closed = (_, __) => Dispatcher.Invoke(() => ReloadNotes()),
            });
        }

        private void ReloadNotes()
        {
            Entity entity = DataContext as Entity;
            if (entity == null || entity.IsNew)
            {
                // lvNotas.ItemsSource = null;
                return;
            }

            var func = CustomFilter.TryGetValue(DataContext.GetType());

            DynamicQueryServer.QueryCountBatch(new QueryCountOptions(typeof(NoteEntity))
            {
                FilterOptions = 
                { 
                    func != null ? func(entity) : new FilterOption("Target", DataContext) { Frozen = true }
                }
            }, count =>
            {
                if (count == 0)
                {
                    tbNotes.FontWeight = FontWeights.Normal;
                    btnExploreNotes.Visibility = Visibility.Collapsed;
                }
                else
                {
                    tbNotes.FontWeight = FontWeights.Bold;
                    btnExploreNotes.FontWeight = FontWeights.Bold;
                    btnExploreNotes.Visibility = Visibility.Visible;
                    btnExploreNotes.Content = count + " " + (count > 1 ? NoteMessage._note.NiceToString() :  NoteMessage._notes.NiceToString());
                }

                if (count > 0 && ForceShow != null)
                    ForceShow();

            }, () => { });
        }
    }
}
