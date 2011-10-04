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
using Signum.Utilities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for Pagination.xaml
    /// </summary>
    public partial class ElementsPerPageSelector : UserControl
    {
        public static readonly DependencyProperty ElementsPerPageProperty =
            DependencyProperty.Register("ElementsPerPage", typeof(int), typeof(ElementsPerPageSelector), new UIPropertyMetadata(0));
        public int ElementsPerPage
        {
            get { return (int)GetValue(ElementsPerPageProperty); }
            set { SetValue(ElementsPerPageProperty, value); }
        }

        public ElementsPerPageSelector()
        {
            InitializeComponent();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
