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
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for Pagination.xaml
    /// </summary>
    public partial class PageSelector : UserControl
    {
        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register("CurrentPage", typeof(int), typeof(PageSelector), new FrameworkPropertyMetadata(1,  FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (s, e) => ((PageSelector)s).RefreshButtons()));
        public int CurrentPage
        {
            get { return (int)GetValue(CurrentPageProperty); }
            set { SetValue(CurrentPageProperty, value); }
        }

        public static readonly DependencyProperty TotalPagesProperty =
            DependencyProperty.Register("TotalPages", typeof(int), typeof(PageSelector), new UIPropertyMetadata(1, 
                (s, e) => ((PageSelector)s).RefreshButtons()));
        public int TotalPages
        {
            get { return (int)GetValue(TotalPagesProperty); }
            set { SetValue(TotalPagesProperty, value); }
        }
        
        public static readonly RoutedEvent PageChangedEvent = EventManager.RegisterRoutedEvent(
            "PageChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PageSelector));
        public event RoutedEventHandler PageChanged
        {
            add { AddHandler(PageChangedEvent, value); }
            remove { RemoveHandler(PageChangedEvent, value); }
        }


        public PageSelector()
        {
            InitializeComponent();

            TotalPages = 100;
            CurrentPage = 50;

            Loaded += new RoutedEventHandler(Pagination_Loaded);
        }

        void Pagination_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshButtons(); 
        }
        
        void RefreshButtons()
        {
            btPrevious.IsEnabled = 1 < CurrentPage;
            btNext.IsEnabled = CurrentPage < TotalPages;

            spPages.Children.Clear();

            MinMax<int> interval = new MinMax<int>(
                Math.Max(1, CurrentPage - 2),
                Math.Min(CurrentPage + 2, TotalPages));

            if (interval.Min != 1)
            {
                spPages.Children.Add(CreateButton(1));
                if (interval.Min - 1 != 1)
                    spPages.Children.Add(CreateLabel("..."));
            }

            for (int i = interval.Min; i < CurrentPage; i++)
                spPages.Children.Add(CreateButton(i));

            spPages.Children.Add(CreateLabel(CurrentPage.ToString()));

            for (int i = CurrentPage + 1; i <= interval.Max; i++) 
                spPages.Children.Add(CreateButton(i));

            if (interval.Max != TotalPages)
            {
                if (interval.Max + 1 != TotalPages)
                    spPages.Children.Add(CreateLabel("..."));
                spPages.Children.Add(CreateButton(TotalPages));
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
            SetPage(CurrentPage - 1);
        }

        private void btNext_Click(object sender, RoutedEventArgs e)
        {
            SetPage(CurrentPage + 1);
        }

        private void spPages_Click(object sender, RoutedEventArgs e)
        {
            SetPage((int)((Button)e.OriginalSource).Content);
        }

        private void SetPage(int currentPage)
        {
            if (1 <= currentPage && currentPage <= TotalPages)
            {
                CurrentPage = currentPage;
                RaiseEvent(new RoutedEventArgs(PageChangedEvent));
            }
        }
    }
}
