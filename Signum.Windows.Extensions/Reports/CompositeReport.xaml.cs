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
using Signum.Windows;
using Signum.Windows.Extensions;
using Signum.Entities.Reports;

namespace Signum.Windows.Reports
{
    /// <summary>
    /// Interaction logic for Pais.xaml
    /// </summary>
    public partial class CompositeReport : UserControl
    {
        public CompositeReport()
        {
            InitializeComponent();
        }

        private void btGenerar_Click(object sender, RoutedEventArgs e)
        {
            CompositeReportDN cr = (CompositeReportDN)this.DataContext;
            ExcelCompositeReportGenerator.GenerateCompositeReport(cr);
        }
    }
}
