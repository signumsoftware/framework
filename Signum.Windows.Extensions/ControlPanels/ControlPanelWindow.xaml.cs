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
using Signum.Entities.ControlPanel;
using Signum.Entities.Reflection;
using Signum.Entities.UserQueries;
using Signum.Utilities;

namespace Signum.Windows.ControlPanels
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

        public static readonly DependencyProperty CurrentEntityProperty =
         DependencyProperty.Register("CurrentEntity", typeof(IdentifiableEntity), typeof(ControlPanelWindow), new PropertyMetadata(null));
        public IdentifiableEntity CurrentEntity
        {
            get { return (IdentifiableEntity)GetValue(CurrentEntityProperty); }
            set { SetValue(CurrentEntityProperty, value); }
        }

        public ControlPanelWindow()
        {
            InitializeComponent();
            this.DataContextChanged += ControlPanelWindow_DataContextChanged;
        }

        void ControlPanelWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
 	        this.tbControlPanel.Text = e.NewValue.TryToString();
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
    }
}
