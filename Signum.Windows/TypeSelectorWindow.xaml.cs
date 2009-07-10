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

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for TypeSelectorWindow.xaml
    /// </summary>
    public partial class TypeSelectorWindow : Window
    {
        public static readonly DependencyProperty TypesProperty =
            DependencyProperty.Register("Types", typeof(Type[]), typeof(TypeSelectorWindow), new UIPropertyMetadata(null));
        public Type[] Types
        {
            get { return (Type[])GetValue(TypesProperty); }
            set { SetValue(TypesProperty, value); }
        }

        public static readonly DependencyProperty SelectedTypeProperty =
            DependencyProperty.Register("SelectedType", typeof(Type), typeof(TypeSelectorWindow), new UIPropertyMetadata(null));
        public Type SelectedType
        {
            get { return (Type)GetValue(SelectedTypeProperty); }
            set { SetValue(SelectedTypeProperty, value); }
        }

        public TypeSelectorWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(TypeSelectorWindow_Loaded);
        }

        void TypeSelectorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Title = Properties.Resources.TypeSelector;
            tb.Text = Properties.Resources.PleaseSelectAType;
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            SelectedType = (Type)((ToggleButton)sender).DataContext;
            DialogResult = true;
            Close(); 
        }
    }
}
