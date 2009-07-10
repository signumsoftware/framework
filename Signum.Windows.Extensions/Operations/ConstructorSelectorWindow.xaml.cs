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
using System.Windows.Controls.Primitives;

namespace Signum.Windows.Operations
{
    /// <summary>
    /// Interaction logic for TypeSelectorWindow.xaml
    /// </summary>
    public partial class ConstructorSelectorWindow : Window
    {
        public static readonly DependencyProperty ConstructorKeysProperty =
            DependencyProperty.Register("ConstructorKeys", typeof(Enum[]), typeof(ConstructorSelectorWindow), new UIPropertyMetadata(null));
        public Enum[] ConstructorKeys
        {
            get { return (Enum[])GetValue(ConstructorKeysProperty); }
            set { SetValue(ConstructorKeysProperty, value); }
        }

        public static readonly DependencyProperty SelectedKeyProperty =
            DependencyProperty.Register("SelectedKey", typeof(Enum), typeof(ConstructorSelectorWindow), new UIPropertyMetadata(null));
        public Enum SelectedKey
        {
            get { return (Enum)GetValue(SelectedKeyProperty); }
            set { SetValue(SelectedKeyProperty, value); }
        }

        public ConstructorSelectorWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(TypeSelectorWindow_Loaded);
        }

        void TypeSelectorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Title = "Constructor Selector";
            tb.Text = "Please Select a Constructor";
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            SelectedKey = (Enum)((ToggleButton)sender).DataContext;
            DialogResult = true;
            Close(); 
        }
    }
}
