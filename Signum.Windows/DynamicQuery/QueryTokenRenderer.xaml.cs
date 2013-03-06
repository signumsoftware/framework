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
using Signum.Utilities;
using System.Windows.Automation;

namespace Signum.Windows
{
    public partial class QueryTokenRenderer : UserControl
    {
        public static readonly DependencyProperty TokenProperty =
            DependencyProperty.Register("Token", typeof(QueryToken), typeof(QueryTokenRenderer),
            new UIPropertyMetadata((d, e) => ((QueryTokenRenderer)d).SetTokens((QueryToken)e.NewValue)));
        public QueryToken Token
        {
            get { return (QueryToken)GetValue(TokenProperty); }
            set { SetValue(TokenProperty, value); }
        }

        private void SetTokens(QueryToken token)
        {
            itemsControl.ItemsSource = token.FollowC(a => a.Parent).Reverse().ToArray();

            AutomationProperties.SetName(this, token.FullKey());
        }
     
        public QueryTokenRenderer()
        {
            InitializeComponent();
        }
    }
}
