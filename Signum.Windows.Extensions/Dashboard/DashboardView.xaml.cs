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
using Signum.Entities.Dashboard;
using Signum.Services;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Windows.Dashboard
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardDN Current
        {
            get { return (DashboardDN)DataContext; }
            set { DataContext = value; }
        }

        public DashboardView()
        {
            InitializeComponent();
        }


        private void GroupBox_Loaded(object sender, RoutedEventArgs e)
        {
            GroupBox gb = (GroupBox)sender;
            PanelPartDN pp = (PanelPartDN)gb.DataContext;
            PartView pv = DashboardClient.PartViews.GetOrThrow(pp.Content.GetType());
            if (pv.OnTitleClick != null && (pv.IsTitleEnabled == null || pv.IsTitleEnabled()))
            {
                TextBlock tb = (TextBlock)gb.FindName("tb");
                tb.Cursor = Cursors.Hand;
                tb.MouseDown += TextBlock_MouseUp;
            }
        }

        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            PanelPartDN pp = (PanelPartDN)tb.DataContext;
            DashboardClient.PartViews.GetOrThrow(pp.Content.GetType()).OnTitleClick(pp.Content);
        }
    }
}
