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
using Signum.Entities.DynamicQuery;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for Pagination.xaml
    /// </summary>
    public partial class ElementsInPageLabel : UserControl
    {
        public static readonly IValueConverter CountToVisibility = ConverterFactory.New((int a) => a == 0 ? Visibility.Collapsed : Visibility.Visible);

        public static readonly DependencyProperty StartElementIndexProperty =
            DependencyProperty.Register("StartElementIndex", typeof(int?), typeof(ElementsInPageLabel), new UIPropertyMetadata(0));
        public int? StartElementIndex
        {
            get { return (int?)GetValue(StartElementIndexProperty); }
            set { SetValue(StartElementIndexProperty, value); }
        }

        public static readonly DependencyProperty EndElementIndexProperty =
            DependencyProperty.Register("EndElementIndex", typeof(int?), typeof(ElementsInPageLabel), new UIPropertyMetadata(0));
        public int? EndElementIndex
        {
            get { return (int?)GetValue(EndElementIndexProperty); }
            set { SetValue(EndElementIndexProperty, value); }
        }

        public static readonly DependencyProperty TotalElementsProperty = 
            DependencyProperty.Register("TotalElements", typeof(int), typeof(ElementsInPageLabel), new UIPropertyMetadata(0));
        public int TotalElements
        {
            get { return (int)GetValue(TotalElementsProperty); }
            set { SetValue(TotalElementsProperty, value); }
        }

        public static readonly DependencyProperty TotalPagesProperty =
          DependencyProperty.Register("TotalPages", typeof(int), typeof(ElementsInPageLabel), new UIPropertyMetadata(0));
        public int TotalPages
        {
            get { return (int)GetValue(TotalPagesProperty); }
            set { SetValue(TotalPagesProperty, value); }
        }

        public ElementsInPageLabel()
        {
            InitializeComponent();
        }
    }
}
