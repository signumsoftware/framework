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
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Reflection;
using Signum.Services;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.Windows.Basics;
using Signum.Entities.UserAssets;

namespace Signum.Windows.UserQueries
{
    public partial class UserQuery : UserControl
    {
        public UserQuery()
        {
            InitializeComponent();
            this.Loaded+=UserQuery_Loaded;
        }

        public static readonly DependencyProperty QueryDescriptionProperty =
            DependencyProperty.Register("QueryDescription", typeof(QueryDescription), typeof(UserQuery), new UIPropertyMetadata(null));
        public QueryDescription QueryDescription
        {
            get { return (QueryDescription)GetValue(QueryDescriptionProperty); }
            set { SetValue(QueryDescriptionProperty, value); }
        }

        void UserQuery_Loaded(object sender, RoutedEventArgs e)
        {
            if (QueryDescription == null)
            {
                UserQueryEntity uq = (UserQueryEntity)DataContext;

                QueryDescription = DynamicQueryServer.GetQueryDescription(QueryClient.GetQueryName(uq.Query.Key));
            }

            tbCurrentEntity.Text = UserQueryMessage.Use0ToFilterCurrentEntity.NiceToString().FormatWith(CurrentEntityConverter.CurrentEntityKey);
        }

        private List<QueryToken> QueryTokenBuilderFilters_SubTokensEvent(QueryToken token)
        {
            return token.SubTokens(QueryDescription, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement);
        }

        private List<QueryToken> QueryTokenBuilder_SubTokensEvent(QueryToken token)
        {
            return token.SubTokens(QueryDescription, SubTokensOptions.CanElement);
        }

        IEnumerable<Lite<Entity>> EntityType_AutoCompleting(string text)
        {
            return TypeClient.ViewableServerTypes().Where(t => t.CleanName.Contains(text, StringComparison.InvariantCultureIgnoreCase)).Select(t => t.ToLite()).Take(5);
        }
    }
}
