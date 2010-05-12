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
using Signum.Entities;

namespace Signum.Windows
{
    public partial class QueryTokenBuilder : UserControl
    {
        public static readonly DependencyProperty TokenProperty =
              DependencyProperty.Register("Token", typeof(QueryToken), typeof(QueryTokenBuilder), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                  (d, e) => ((QueryTokenBuilder)d).UpdateTokenList((QueryToken)e.NewValue)));
        public QueryToken Token
        {
            get { return (QueryToken)GetValue(TokenProperty); }
            set { SetValue(TokenProperty, value); }
        }

        List<QueryToken> tokens = new List<QueryToken>(); 
        private void UpdateTokenList(QueryToken queryToken)
        {
            if (tokens.LastOrDefault() == queryToken)
                return;

            if (queryToken == null)
                tokens = new List<QueryToken>();
            else
                tokens = queryToken.FollowC(a => a.Parent).Reverse().ToList();
            UpdateCombo();
        }


        public static readonly DependencyProperty StaticColumnsProperty =
            DependencyProperty.Register("StaticColumns", typeof(IEnumerable<StaticColumn>), typeof(QueryTokenBuilder), new UIPropertyMetadata(null,
                (d, e) => ((QueryTokenBuilder)d).UpdateCombo()));
        public IEnumerable<StaticColumn> StaticColumns
        {
            get { return (IEnumerable<StaticColumn>)GetValue(StaticColumnsProperty); }
            set { SetValue(StaticColumnsProperty, value); }
        }


        void UpdateCombo()
        {
            if (StaticColumns == null)
                return; 

            sp.Children.Clear(); 
            for (int i = 0; i < tokens.Count + 1; i++)
			{
                QueryToken[] subTokens = i == 0 ? StaticColumns.Select(c => QueryToken.NewColumn(c)).ToArray() :
                                                 tokens[i-1].SubTokens();

                if (i == tokens.Count && subTokens == null || subTokens.Length == 0)
                    break;

                int index = i == tokens.Count ? -1: Array.FindIndex(subTokens, a=>a.Key == tokens[i].Key);

                ComboBox cb = new ComboBox()
                {
                    Tag = i,
                    ItemsSource = subTokens,
                    SelectedIndex = index,
                };
                sp.Children.Add(cb); 
			}
        }

        void cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)e.OriginalSource;
            int index = (int)cb.Tag;
            QueryToken newToken = (QueryToken)cb.SelectedItem;
            QueryToken[] subTokens = newToken.SubTokens();

            sp.Children.RemoveRange(index + 1, sp.Children.Count - (index + 1)); //all
            if (subTokens != null && subTokens.Length != 0)
                sp.Children.Add(new ComboBox
                {
                    Tag = index + 1,
                    ItemsSource = subTokens,
                    SelectedIndex = -1,
                });

            if (tokens.Count <= index)
                tokens.Add(newToken);
            else
            {
                tokens[index] = newToken;
                tokens.RemoveRange(index + 1, tokens.Count - (index + 1)); //all
            }

            Token = tokens.LastOrDefault();
        }

        public QueryTokenBuilder()
        {
            InitializeComponent();
            sp.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(cb_SelectionChanged));
        }

        private void sp_DragOver(object sender, DragEventArgs e)
        {
             e.Effects = e.Data.GetDataPresent(typeof(FilterOption)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void sp_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FilterOption)))
            {
                FilterOption filter = (FilterOption)e.Data.GetData(typeof(FilterOption));

                Token = filter.Token;
            }
        }
    }
}
