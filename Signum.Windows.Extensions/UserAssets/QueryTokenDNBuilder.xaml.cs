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
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;

namespace Signum.Windows.UserAssets
{
    /// <summary>
    /// Interaction logic for QueryTokenDNBuilder.xaml
    /// </summary>
    public partial class QueryTokenDNBuilder : UserControl
    {
        public event Func<QueryToken, List<QueryToken>> SubTokensEvent;

        public static readonly DependencyProperty TokenProperty = 
            DependencyProperty.Register("Token", typeof(QueryTokenDN), typeof(QueryTokenDNBuilder), new PropertyMetadata(null));
        public QueryTokenDN Token
        {
            get { return (QueryTokenDN)GetValue(TokenProperty); }
            set { SetValue(TokenProperty, value); }
        }

        public QueryTokenDNBuilder()
        {
            InitializeComponent();
        }

        static QueryTokenDNBuilder()
        {
            Common.ValuePropertySelector.SetDefinition(typeof(QueryTokenDNBuilder), TokenProperty);
        }

        private List<QueryToken> QueryTokenBuilder_SubTokensEvent(QueryToken arg)
        {
            if (SubTokensEvent == null)
                throw new InvalidOperationException("SubTokensEvent not set");

            return SubTokensEvent(arg);
        }

        public void UpdateTokenList()
        {
            tokenBuilder.UpdateTokenList();
        }
    }
}
