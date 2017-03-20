using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Windows.Help
{
    /// <summary>
    /// Interaction logic for HelpButton.xaml
    /// </summary>
    public partial class HelpButton : UserControl
    {
        public static readonly DependencyProperty IsActiveProperty =
          DependencyProperty.Register("IsActive", typeof(bool), typeof(HelpButton), new PropertyMetadata(false, (o,e)=>((HelpButton)o).IsActiveChanged((bool)e.NewValue)));
        
        private void IsActiveChanged(bool isActive)
        {
            if (isActive)
            {
                button.BorderBrush = Brushes.DarkOrange;
                button.BorderThickness = new Thickness(2);
                button.Foreground = Brushes.DarkOrange;
            }
            else
            {
                button.ClearValue(BorderBrushProperty);
                button.ClearValue(BorderThicknessProperty);
                button.ClearValue(ForegroundProperty);
            }
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public bool? IsChecked
        {
            get { return this.button.IsChecked; }
        }

        public static readonly DependencyProperty MainControlProperty =
            DependencyProperty.Register("MainControl", typeof(Control), typeof(HelpButton), new PropertyMetadata(null));
        public Control MainControl
        {
            get { return (Control)GetValue(MainControlProperty); }
            set { SetValue(MainControlProperty, value); }
        }

        public event EventHandler Checked;

        public HelpButton()
        {
            InitializeComponent();
        }

        public const WhereFlags WhereFlags =
            Signum.Windows.WhereFlags.Recursive |
            Signum.Windows.WhereFlags.VisualTree |
            Signum.Windows.WhereFlags.StartOnParent;

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            Checked?.Invoke(this, null);
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var controls = MainControl.Children<Control>(c => HelpClient.GetHelpInfo(c) != null, WhereFlags);
            foreach (var item in controls)
            {
                HelpClient.SetHelpInfo(item, null);
            }
        }
    }
}