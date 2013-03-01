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
using Signum.Entities.DynamicQuery;
using Signum.Entities.Chart;
using Signum.Entities.Reports;
using Signum.Entities;
using Signum.Entities.UserQueries;

namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartRequest.xaml
    /// </summary>
    public partial class UserChart : UserControl
    {
        public static readonly DependencyProperty QueryDescriptionProperty =
           DependencyProperty.Register("QueryDescription", typeof(QueryDescription), typeof(UserChart), new UIPropertyMetadata(null));
        public QueryDescription QueryDescription
        {
            get { return (QueryDescription)GetValue(QueryDescriptionProperty); }
            set { SetValue(QueryDescriptionProperty, value); }
        }

        public UserChart()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(UserChart_Loaded);
        }

        void UserChart_Loaded(object sender, RoutedEventArgs e)
        {
            if (QueryDescription == null)
            {
                UserChartDN uq = (UserChartDN)DataContext;

                QueryDescription = DynamicQueryClient.GetQueryDescription(QueryClient.GetQueryName(uq.Query.Key));
            }
            chartBuilder.Description = QueryDescription;
        }

        private List<QueryToken> QueryTokenBuilderFilter_SubTokensEvent(QueryToken token)
        {
            var cr = (UserChartDN)DataContext;
            if (cr == null || QueryDescription == null)
                return new List<QueryToken>();

            return token.SubTokensChart(QueryDescription.Columns, cr.GroupResults);
        }

        private List<QueryToken> QueryTokenBuilderOrders_SubTokensEvent(QueryToken token)
        {
            var cr = (UserChartDN)DataContext;
            if (cr == null || QueryDescription == null)
                return new List<QueryToken>();

            return token.SubTokensChart(QueryDescription.Columns, cr.GroupResults);
        }
    }
}
