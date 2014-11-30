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
using Signum.Entities;
using Signum.Entities.UserQueries;
using Signum.Windows.Basics;
using Signum.Utilities;
using Signum.Entities.UserAssets;

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
                UserChartEntity uq = (UserChartEntity)DataContext;

                QueryDescription = DynamicQueryServer.GetQueryDescription(QueryClient.GetQueryName(uq.Query.Key));
            }
            chartBuilder.Description = QueryDescription;

            tbCurrentEntity.Text = UserQueryMessage.Use0ToFilterCurrentEntity.NiceToString().FormatWith(CurrentEntityConverter.CurrentEntityKey);
        }

        private List<QueryToken> QueryTokenBuilderFilter_SubTokensEvent(QueryToken token)
        {
            var cr = (UserChartEntity)DataContext;
            if (cr == null || QueryDescription == null)
                return new List<QueryToken>();

            return token.SubTokens(QueryDescription, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement  | (cr.GroupResults ? SubTokensOptions.CanAggregate : 0));
        }

        private List<QueryToken> QueryTokenBuilderOrders_SubTokensEvent(QueryToken token)
        {
            var cr = (UserChartEntity)DataContext;
            if (cr == null || QueryDescription == null)
                return new List<QueryToken>();

            return token.SubTokens(QueryDescription, SubTokensOptions.CanElement | (cr.GroupResults ? SubTokensOptions.CanAggregate : 0));
        }

        IEnumerable<Lite<Entity>> EntityType_AutoCompleting(string text)
        {
            return TypeClient.ViewableServerTypes().Where(t => t.CleanName.Contains(text, StringComparison.InvariantCultureIgnoreCase)).Select(t => t.ToLite()).Take(5);
        }
    }
}
