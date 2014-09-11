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
using System.Windows.Shapes;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dashboard;
using Signum.Entities.Reflection;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.Windows.Authorization;

namespace Signum.Windows.Dashboard
{
    /// <summary>
    /// Interaction logic for DashboardWindow.xaml
    /// </summary>
    public partial class DashboardWindow : Window
    {
        public DashboardDN Current
        {
            get { return (DashboardDN)DataContext; }
            set { DataContext = value; }
        }

        public DashboardWindow()
        {   
            DashboardPermission.ViewDashboard.Authorize();
            InitializeComponent();
            this.DataContextChanged += DashboardWindow_DataContextChanged;
        }

        void DashboardWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool canNavigate = (e.NewValue != null && Navigator.IsNavigable((IIdentifiable)e.NewValue, isSearch: true));
            this.tbDashboard.Cursor = canNavigate ? Cursors.Hand : Cursors.Arrow;   
 	        this.tbDashboard.Text = e.NewValue.TryToString();
        }

        private void reload_Click(object sender, RoutedEventArgs e)
        {
            var fresh = Current.ToLite().Retrieve();
            Current = null;
            Current = fresh;
        }

        private void tbDashboard_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Navigator.IsNavigable(Current, isSearch: true))
                Navigator.Navigate(Current.ToLite());
        }
    }
}
