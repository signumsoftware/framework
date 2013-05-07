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
using Signum.Entities.ControlPanel;
using Signum.Utilities;

namespace Signum.Windows.Extensions.ControlPanels
{
    /// <summary>
    /// Interaction logic for ControlPanelWindow.xaml
    /// </summary>
    public partial class ControlPanelWindow : Window
    {
        public ControlPanelDN Current
        {
            get { return (ControlPanelDN)DataContext; }
            set { DataContext = value; }
        }

        public ControlPanelWindow()
        {
            InitializeComponent();
        }

        private void reload_Click(object sender, RoutedEventArgs e)
        {
            var fresh = Current.ToLite().Retrieve();
            Current = null;
            Current = fresh;
        }

        private void navigate_Click(object sender, RoutedEventArgs e)
        {
            Navigator.Navigate(Current.ToLite());
        }

        public static void View(Lite<ControlPanelDN> controlPanel)
        {
            ControlPanelWindow win = new ControlPanelWindow();

            win.tbEntityId.Text = NormalWindowMessage.Loading0.NiceToString().Formato(controlPanel.EntityType.NiceName());
            win.Show();

            win.DataContext = controlPanel.Retrieve();
        }
    }
}
