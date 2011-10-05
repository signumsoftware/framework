using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Linq;
using Signum.Entities;
using Signum.Utilities;
using System.ComponentModel;
using System.Windows.Threading;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Signum.Entities.DynamicQuery;

namespace Signum.Windows
{
    public partial class SearchWindow
    {
        public static readonly DependencyProperty QueryNameProperty =
            DependencyProperty.Register("QueryName", typeof(object), typeof(SearchWindow), new UIPropertyMetadata(null));
        public object QueryName
        {
            get { return (object)GetValue(QueryNameProperty); }
            set { SetValue(QueryNameProperty, value); }
        }
    
        public static readonly DependencyProperty FilterOptionsProperty =
          DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOption>), typeof(SearchWindow), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> FilterOptions
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
        }

        public static readonly DependencyProperty OrderOptionsProperty =
             DependencyProperty.Register("OrderOptions", typeof(ObservableCollection<OrderOption>), typeof(SearchWindow), new UIPropertyMetadata(null));
        public ObservableCollection<OrderOption> OrderOptions
        {
            get { return (ObservableCollection<OrderOption>)GetValue(OrderOptionsProperty); }
            set { SetValue(OrderOptionsProperty, value); }
        }

        public static readonly DependencyProperty ColumnOptionsModeProperty =
          DependencyProperty.Register("ColumnOptionsMode", typeof(ColumnOptionsMode), typeof(SearchWindow), new UIPropertyMetadata(ColumnOptionsMode.Add));
        public ColumnOptionsMode ColumnOptionsMode
        {
            get { return (ColumnOptionsMode)GetValue(ColumnOptionsModeProperty); }
            set { SetValue(ColumnOptionsModeProperty, value); }
        }

        public static readonly DependencyProperty ColumnOptionsProperty =
            DependencyProperty.Register("ColumnOptions", typeof(ObservableCollection<ColumnOption>), typeof(SearchWindow), new UIPropertyMetadata(null));
        public ObservableCollection<ColumnOption> ColumnOptions
        {
            get { return (ObservableCollection<ColumnOption>)GetValue(ColumnOptionsProperty); }
            set { SetValue(ColumnOptionsProperty, value); }
        }

        public static readonly DependencyProperty AllowUserColumnsProperty =
            DependencyProperty.Register("AllowUserColumns", typeof(bool), typeof(SearchWindow), new UIPropertyMetadata(true));
        public bool AllowUserColumns
        {
            get { return (bool)GetValue(AllowUserColumnsProperty); }
            set { SetValue(AllowUserColumnsProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
          DependencyProperty.Register("SelectedItem", typeof(Lite), typeof(SearchWindow), new UIPropertyMetadata(null));
        public Lite SelectedItem
        {
            get { return (Lite)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
          DependencyProperty.Register("SelectedItems", typeof(Lite[]), typeof(SearchWindow), new UIPropertyMetadata(null));
        public Lite[] SelectedItems
        {
            get { return (Lite[])GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty MultiSelectionProperty =
            DependencyProperty.Register("MultiSelection", typeof(bool), typeof(SearchWindow), new UIPropertyMetadata(true));
        public bool MultiSelection
        {
            get { return (bool)GetValue(MultiSelectionProperty); }
            set { SetValue(MultiSelectionProperty, value); }
        }

        public static readonly DependencyProperty ShowFiltersProperty =
            DependencyProperty.Register("ShowFilters", typeof(bool), typeof(SearchWindow), new UIPropertyMetadata(true));
        public bool ShowFilters
        {
            get { return (bool)GetValue(ShowFiltersProperty); }
            set { SetValue(ShowFiltersProperty, value); }
        }

        public static readonly DependencyProperty ShowFilterButtonProperty =
            DependencyProperty.Register("ShowFilterButton", typeof(bool), typeof(SearchWindow), new UIPropertyMetadata(true));
        public bool ShowFilterButton
        {
            get { return (bool)GetValue(ShowFilterButtonProperty); }
            set { SetValue(ShowFilterButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowHeaderProperty =
            DependencyProperty.Register("ShowHeader", typeof(bool), typeof(SearchWindow), new UIPropertyMetadata(true));
        public bool ShowHeader
        {
            get { return (bool)GetValue(ShowHeaderProperty); }
            set { SetValue(ShowHeaderProperty, value); }
        }

        public static readonly DependencyProperty ShowFooterProperty =
            DependencyProperty.Register("ShowFooter", typeof(bool), typeof(SearchWindow), new UIPropertyMetadata(true));
        public bool ShowFooter
        {
            get { return (bool)GetValue(ShowFooterProperty); }
            set { SetValue(ShowFooterProperty, value); }
        }

        public SearchMode Mode { get; private set; }
        public bool SearchOnLoad { get; private set; }

        public SearchWindow(SearchMode mode, bool seachOnLoad)
        {
            this.InitializeComponent();

            searchControl.SearchOnLoad = seachOnLoad;
            this.SearchOnLoad = seachOnLoad;
            this.Mode = mode;

            ButtonsChanged();
            searchControl.Loaded += new RoutedEventHandler(searchControl_Loaded);
            searchControl.ClearSize += new EventHandler(searchControl_ClearSize);
        }

        void searchControl_ClearSize(object sender, EventArgs e)
        {
            ClearValue(WidthProperty);
            ClearValue(HeightProperty);
            this.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
        }

        public SearchWindow()
        {
            this.InitializeComponent();
            ButtonsChanged();
            searchControl.Loaded += new RoutedEventHandler(searchControl_Loaded);
        }

        void searchControl_Loaded(object sender, RoutedEventArgs e)
        {
            tbEntityType.Text = searchControl.EntityType.NicePluralName();

            string niceQueryName = QueryUtils.GetNiceName(QueryName);

            if (niceQueryName.StartsWith(tbEntityType.Text))
                niceQueryName = niceQueryName.Substring(tbEntityType.Text.Length).Trim();
            else
                niceQueryName = "- " + niceQueryName;

            tbQueryName.Text = niceQueryName;
        }

        void ButtonsChanged()
        {
            bool ok = Mode == SearchMode.Find;

            btOk.Visibility = ok ? Visibility.Visible : Visibility.Collapsed;
            btCancel.Visibility = ok ? Visibility.Visible : Visibility.Collapsed;
            
            searchControl.IsAdmin = !ok;

            if (ok)
            {
                searchControl.DoubleClick += new Action(searchControl_DoubleClick);
            }
            else
            {
                searchControl.DoubleClick -= new Action(searchControl_DoubleClick);
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                searchControl.Search();

                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            OkAndClose();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OkAndClose()
        {
            if (MultiSelection)
                SelectedItems = searchControl.SelectedItems;
            else
                SelectedItem = searchControl.SelectedItem;

            DialogResult = true;
        }

        void searchControl_DoubleClick()
        {
            OkAndClose();
        }
    }

    public enum SearchMode
    {
        Find,
        Explore
    }
}