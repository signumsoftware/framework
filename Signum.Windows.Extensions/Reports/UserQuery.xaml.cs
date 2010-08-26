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
using Signum.Entities.Reports;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Reflection;
using Signum.Services;

namespace Signum.Windows.Reports
{
    public partial class UserQuery : UserControl
    {
        public UserQuery()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty QueryDescriptionProperty =
            DependencyProperty.Register("QueryDescription", typeof(QueryDescription), typeof(UserQuery), new UIPropertyMetadata(null));
        public QueryDescription QueryDescription
        {
            get { return (QueryDescription)GetValue(QueryDescriptionProperty); }
            set { SetValue(QueryDescriptionProperty, value); }
        }

        public static UserQueryDN FromSearchControl(SearchControl searchControl)
        {
            return new UserQueryDN
            {
                 Query = QueryClient.GetQuery(searchControl.QueryName),

                 Filters = searchControl.FilterOptions.Select(fo => new QueryFilterDN
                 {
                     Token = fo.Token,
                     Operation = fo.Operation,
                     ValueString = FilterValueConverter.ToString(fo.RealValue, fo.Token.Type),
                 }).ToMList(),

                 Columns = searchControl.UserColumns.Select(uco=>new QueryColumnDN
                 { 
                     DisplayName = uco.UserColumn.DisplayName,
                     Token = uco.UserColumn.Token,
                 }).ToMList(),

                 Orders = searchControl.OrderOptions.Select((fo,i) => new QueryOrderDN
                 {
                     Index = i,
                     Token = fo.Token,
                     OrderType = fo.OrderType,
                 }).ToMList()
            };
        }


        private QueryToken[] QueryTokenBuilderFilter_SubTokensEvent(QueryToken token)
        {
            return QueryUtils.SubTokensFilter(token, QueryDescription.StaticColumns);
        }

        private QueryToken[] QueryTokenBuilderOrder_SubTokensEvent(QueryToken token)
        {
            return QueryUtils.SubTokensOrder(token, QueryDescription.StaticColumns);
        }

        private QueryToken[] QueryTokenBuilderColumn_SubTokensEvent(QueryToken token)
        {
            return QueryUtils.SubTokensColumn(token, QueryDescription.StaticColumns);
        }
    }
}
