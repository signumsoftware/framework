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
            this.Loaded += new RoutedEventHandler(ControlPanelView_Loaded);
            
        }

        void ControlPanelView_Loaded(object sender, RoutedEventArgs e)
        {
            cbCombox.ItemsSource = Server.RetrieveAllLite<ControlPanelDN>();

            var home = Server.Return((IControlPanelServer cps) => cps.GetHomePageControlPanel());

            Current = home;
            cbCombox.SelectedItem = home.ToLite();
        }

        private void cbCombox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Lite<ControlPanelDN> cp = cbCombox.SelectedItem as Lite<ControlPanelDN>;

            if (cp != null && !cp.RefersTo(Current))
            {
                Current = cp.Retrieve(); 
            }
        }
    }
}
