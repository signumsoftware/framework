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
using Signum.Entities;
using Signum.Entities.ControlPanel;
using Signum.Windows.Chart;
using Signum.Entities.Chart;
using Signum.Services;

namespace Signum.Windows.ControlPanels
{
    public partial class UserChartPartView : UserControl
    {
        public UserChartPartView()
        {
            InitializeComponent();
            this.chartRenderer.GenerateChart += chartRenderer_GenerateChart;
            this.Loaded += UserChartPartView_Loaded;
        }

        void UserChartPartView_Loaded(object sender, RoutedEventArgs e)
        {
            var dc = (UserChartPartDN)DataContext;
            chartRenderer.FilterOptions = new FreezableCollection<FilterOption>();
            chartRenderer.DataContext = dc.UserChart.ToRequest();
            if (dc.ShowData)
                chartRenderer.ShowData(); 
            chartRenderer.UpdateFiltersOrdersUserInterface();
            chartRenderer.GenerateOnLoad = true;
        }

        void chartRenderer_GenerateChart()
        {
            chartRenderer.Request.Filters = chartRenderer.FilterOptions.Select(f => f.ToFilter()).ToList();
            chartRenderer.Request.Orders = chartRenderer.OrderOptions.Select(o => o.ToOrder()).ToList();

            var request = chartRenderer.Request;

            Async.Do(
                () => chartRenderer.ResultTable = Server.Return((IChartServer cs) => cs.ExecuteChart(request)),
                () =>
                {
                    request.NeedNewQuery = false;
                    chartRenderer.ReDrawChart();
                },
                () => { });
        }
    }
}
