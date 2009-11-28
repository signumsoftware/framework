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

        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register("Result", typeof(object), typeof(SearchWindow), new UIPropertyMetadata(null));
        public object Result
        {
            get { return (object)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
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

        public SearchButtons Buttons { get; private set; }
        public OnLoadMode OnLoad { get; private set; }

        public SearchWindow(SearchButtons buttons, OnLoadMode onLoad)
        {
            this.InitializeComponent();

            this.OnLoad = onLoad;
            this.Buttons = buttons;
            if (buttons == SearchButtons.Close && onLoad == OnLoadMode.SearchAndReturnIfOne)
                throw new InvalidOperationException("Invalid SearchWindows configuration, Close and SearchAndReturnIfOne don't work together");

            switch (onLoad)
            {
                case OnLoadMode.None:
                    break;
                case OnLoadMode.Search:
                    searchControl.SearchOnLoad = true;
                    break;
                case OnLoadMode.SearchAndReturnIfOne:
                    {
                        searchControl.QueryResultChanged += new RoutedEventHandler(searchControl_QueryResultChanged);
                        searchControl.SearchOnLoad = true;
                        break;
                    }
            }

            ButtonsChanged();
        }

        public SearchWindow()
        {
            this.InitializeComponent();
            ButtonsChanged();
        }

        void ButtonsChanged()
        {
            bool okCancel = Buttons == SearchButtons.OkCancel;

            btOk.Visibility = okCancel ? Visibility.Visible : Visibility.Collapsed;
            btCancel.Visibility = okCancel ? Visibility.Visible : Visibility.Collapsed;
            
            btClose.Visibility = !okCancel ? Visibility.Visible : Visibility.Collapsed;
            searchControl.IsAdmin = !okCancel;

            if (okCancel)
            {
                searchControl.DoubleClick += new Action(searchControl_DoubleClick);
            }
            else
            {
                searchControl.DoubleClick -= new Action(searchControl_DoubleClick);
            }
        }

        void searchControl_QueryResultChanged(object sender, RoutedEventArgs e)
        {
            if (this.OnLoad == OnLoadMode.SearchAndReturnIfOne && searchControl.QueryResult != null)
            {
                if (searchControl.QueryResult.Data.Length <= 1)
                    OkAndClose();
                else
                    searchControl.QueryResultChanged -= new RoutedEventHandler(searchControl_QueryResultChanged);
            }
        }

        public List<Filter> CurrentFilters()
        {
            return searchControl.CurrentFilters();
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

        private void OkAndClose()
        {
            if (MultiSelection)
                Result = searchControl.SelectedItems;
            else
                Result = searchControl.SelectedItem;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void searchControl_DoubleClick()
        {
            OkAndClose();
        }
    }
}