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

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for NotesWidget.xaml
    /// </summary>
    public partial class NotesWidget : UserControl, IWidget
    {
        public event Action ForceShow;

        public static Func<IdentifiableEntity, INoteDN> CreateNote { get; set; }

        public static object NotesQuery { get; set; }
        public static string NotesQueryColumn { get; set; }
        public static string NotesQueryOrder { get; set; }
        public static OrderType? NotesQueryOrderType { get; set; }

        public NotesWidget()
        {
            InitializeComponent();

            // lvNotas.AddHandler(Button.ClickEvent, new RoutedEventHandler(Note_MouseDown));
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
                Lite<INoteDN> nota = (Lite<INoteDN>)b.Tag;
                ViewNote(Server.RetrieveAndForget(nota));
            }
        }

        private void btnNewNote_Click(object sender, RoutedEventArgs e)
        {
            if (CreateNote == null)
                throw new ArgumentNullException("NotesWidget.CreateNote");

            if (DataContext == null)
                return;

            INoteDN nota = CreateNote((IdentifiableEntity)DataContext);

            ViewNote(nota);
        }

        private void btnExploreNotes_Click(object sender, RoutedEventArgs e)
        {
            Navigator.Explore(new ExploreOptions(NotesQuery)
            {
                ShowFilters = false,
                SearchOnLoad = true,
                FilterOptions = { new FilterOption(NotesQueryColumn, DataContext) { Frozen = true } },
                ColumnOptions = { new ColumnOption(NotesQueryColumn) },
                ColumnOptionsMode = ColumnOptionsMode.Remove,
                OrderOptions = NotesQueryOrder.HasText() 
                    ? new List<OrderOption> 
                    { 
                        new OrderOption(NotesQueryOrder, NotesQueryOrderType.HasValue ? NotesQueryOrderType.Value : OrderType.Ascending) 
                    } 
                    : null,
                Closed = (_, __) => ReloadNotes()
            });
        }

        void ViewNote(INoteDN note)
        {
            Navigator.NavigateUntyped(note, new NavigateOptions()
            {
                Closed = (_, __) => ReloadNotes(),
            });
        }

        private void ReloadNotes()
        {
            if (CreateNote == null)
                throw new ArgumentNullException("NotesWidget.RetrieveNotes");

            IdentifiableEntity entity = DataContext as IdentifiableEntity;
            if (entity == null || entity.IsNew)
            {
                // lvNotas.ItemsSource = null;
                return;
            }

            Navigator.QueryCountBatch(new CountOptions(NotesQuery)
            {
                FilterOptions = { new FilterOption(NotesQueryColumn, DataContext) }
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
                    btnExploreNotes.Content = count + " " + (count > 1 ? Properties.Resources._notes : Properties.Resources._note);
                }

                if (count > 0 && ForceShow != null)
                    ForceShow();
            }, () => { });
        }
    }
}
