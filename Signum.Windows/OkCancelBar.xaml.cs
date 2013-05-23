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

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for OkCancelBar.xaml
    /// </summary>
    public partial class OkCancelBar : UserControl
    {
        public static readonly RoutedEvent OkClickedEvent = EventManager.RegisterRoutedEvent(
            "OkClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(OkCancelBar));
        public event RoutedEventHandler OkClicked
        {
            add { AddHandler(OkClickedEvent, value); }
            remove { RemoveHandler(OkClickedEvent, value); }
        }

        public static readonly RoutedEvent CancelClickedEvent = EventManager.RegisterRoutedEvent(
            "CancelClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(OkCancelBar));
        public event RoutedEventHandler CancelClicked
        {
            add { AddHandler(CancelClickedEvent, value); }
            remove { RemoveHandler(CancelClickedEvent, value); }
        }

        public OkCancelBar()
        {
            InitializeComponent();
        }

        private void btOk_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(OkClickedEvent));
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CancelClickedEvent));
        }
    }
}
