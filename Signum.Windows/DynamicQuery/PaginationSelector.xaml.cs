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
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Windows
{
    public partial class PaginationSelector : UserControl
    {
        public static readonly DependencyProperty PaginationProperty =
            DependencyProperty.Register("Pagination", typeof(Pagination), typeof(PaginationSelector), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (s, e) => ((PaginationSelector)s).PaginationChanged()));
        public Pagination Pagination
        {
            get { return (Pagination)GetValue(PaginationProperty); }
            set { SetValue(PaginationProperty, value); }
        }

        public static readonly DependencyProperty TotalPagesProperty =
          DependencyProperty.Register("TotalPages", typeof(int?), typeof(PaginationSelector), new PropertyMetadata(null, (s, e) => ((PaginationSelector)s).PaginationChanged()));
        public int? TotalPages
        {
            get { return (int?)GetValue(TotalPagesProperty); }
            set { SetValue(TotalPagesProperty, value); }
        }

        public static readonly RoutedEvent PageChangedEvent = EventManager.RegisterRoutedEvent(
            "PageChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PaginationSelector));
        public event RoutedEventHandler PageChanged
        {
            add { AddHandler(PageChangedEvent, value); }
            remove { RemoveHandler(PageChangedEvent, value); }
        }

        public PaginationSelector()
        {
            InitializeComponent();

            cbMode.ItemsSource = EnumExtensions.GetValues<PaginationMode>();
            cbMode.ItemTemplate = ValueLineConfigurator.ComboEnumDescriptionTemplate;

            cbElements.ItemsSource = new List<int> { 5, 10, 20, 50, 100, 200 };

            Loaded += new RoutedEventHandler(Pagination_Loaded);
        }

        void Pagination_Loaded(object sender, RoutedEventArgs e)
        {
            PaginationChanged();
        }
        bool loading = false;
        private void PaginationChanged()
        {
            try
            {
                loading = true;

                cbMode.SelectedItem = Pagination?.GetMode();

                if (Pagination is Pagination.Paginate || Pagination is Pagination.Firsts)
                {
                    cbElements.Visibility = System.Windows.Visibility.Visible;
                    cbElements.SelectedItem = Pagination.GetElementsPerPage();
                }
                else
                {
                    cbElements.Visibility = System.Windows.Visibility.Collapsed;
                }

                if (Pagination is Pagination.Paginate && TotalPages != null)
                {
                    spPageSelector.Visibility = System.Windows.Visibility.Visible;
                    RefreshPageSelector();
                }
                else
                {
                    spPageSelector.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            finally
            {
                loading = false;
            }
        }

        void RefreshPageSelector()
        {
            var paginate = (Pagination.Paginate)Pagination;
            var totalPages = TotalPages.Value;

            btPrevious.IsEnabled = 1 < paginate.CurrentPage;
            btNext.IsEnabled = paginate.CurrentPage < totalPages;

            spPages.Children.Clear();

            MinMax<int> interval = new MinMax<int>(
                Math.Max(1, paginate.CurrentPage - 2),
                Math.Min(paginate.CurrentPage + 2, totalPages));

            if (interval.Min != 1)
            {
                spPages.Children.Add(CreateButton(1));
                if (interval.Min - 1 != 1)
                    spPages.Children.Add(CreateLabel("..."));
            }

            for (int i = interval.Min; i < paginate.CurrentPage; i++)
                spPages.Children.Add(CreateButton(i));

            spPages.Children.Add(CreateLabel(paginate.CurrentPage.ToString()));

            for (int i = paginate.CurrentPage + 1; i <= interval.Max; i++)
                spPages.Children.Add(CreateButton(i));

            if (interval.Max != totalPages)
            {
                if (interval.Max + 1 != totalPages)
                    spPages.Children.Add(CreateLabel("..."));
                spPages.Children.Add(CreateButton(totalPages));
            }
        }

        private UIElement CreateButton(int p)
        {
            return new Button { Content = p };
        }

        private UIElement CreateLabel(string p)
        {
            return new Label { Content = p };
        }

        private void btPrevious_Click(object sender, RoutedEventArgs e)
        {
            SetPage(((Pagination.Paginate)Pagination).CurrentPage - 1);
        }

        private void btNext_Click(object sender, RoutedEventArgs e)
        {
            SetPage(((Pagination.Paginate)Pagination).CurrentPage + 1);
        }

        private void spPages_Click(object sender, RoutedEventArgs e)
        {
            SetPage((int)((Button)e.OriginalSource).Content);
        }

        private void SetPage(int currentPage)
        {
            if (1 <= currentPage && currentPage <= TotalPages)
            {
                Pagination = ((Pagination.Paginate)Pagination).WithCurrentPage(currentPage);
                RaiseEvent(new RoutedEventArgs(PageChangedEvent));
            }
        }

        private void cbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loading)
                return;

            var mode = (PaginationMode)cbMode.SelectedItem;

            Pagination =
                mode == PaginationMode.All ? new Pagination.All() :
                mode == PaginationMode.Firsts ? new Pagination.Firsts(Pagination.Firsts.DefaultTopElements) :
                mode == PaginationMode.Paginate ? new Pagination.Paginate(Pagination.Paginate.DefaultElementsPerPage, 1) : (Pagination)null;
        }

        private void cbElements_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loading)
                return;

            switch (Pagination.GetMode())
            {
                case PaginationMode.All:
                    break;
                case PaginationMode.Firsts:
                    Pagination = new Pagination.Firsts((int)cbElements.SelectedItem);
                    break;
                case PaginationMode.Paginate:
                    Pagination = new Pagination.Paginate((int)cbElements.SelectedItem, 1);
                    break;
                default:
                    break;
            }


        }
    }
}
