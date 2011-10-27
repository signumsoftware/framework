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

namespace Signum.Windows.UserQueries
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


        private List<QueryToken> QueryTokenBuilder_SubTokensEvent(QueryToken token)
        {
            return QueryUtils.SubTokens(token, QueryDescription.Columns);
        }
    }
}
