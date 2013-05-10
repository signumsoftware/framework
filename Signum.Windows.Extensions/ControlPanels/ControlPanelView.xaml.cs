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
using Signum.Entities.ControlPanel;
using Signum.Services;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Windows.ControlPanels
{
    /// <summary>
    /// Interaction logic for ControlPanelView.xaml
    /// </summary>
    public partial class ControlPanelView : UserControl
    {
        public ControlPanelDN Current
        {
            get { return (ControlPanelDN)DataContext; }
            set { DataContext = value; }
        }

        public ControlPanelView()
        {
            InitializeComponent();
        }


        private void GroupBox_Loaded(object sender, RoutedEventArgs e)
        {
            GroupBox gb = (GroupBox)sender;
            PanelPartDN pp = (PanelPartDN)gb.DataContext;
            PartView pv = ControlPanelClient.PartViews.GetOrThrow(pp.Content.GetType());
            if (pv.OnTitleClick != null)
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
            ControlPanelClient.PartViews.GetOrThrow(pp.Content.GetType()).OnTitleClick(pp.Content);
        }
    }
}
