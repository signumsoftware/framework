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
        static QueryTokenBuilder()
        {
            PropertyRoute.SetFindImplementationsCallback(Server.FindImplementations);
        }

        List<QueryToken> tokens; 
        public QueryToken Token
        {
            get { return tokens.LastOrDefault(); }
            set
            {
                if (value == null)
                    tokens = new List<QueryToken>();
                else
                    tokens = value.FollowC(a => a.Parent).Reverse().ToList();
                UpdateTokens();
            }
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns", typeof(List<StaticColumn>), typeof(QueryTokenBuilder), new UIPropertyMetadata((d, e) => ((QueryTokenBuilder)d).UpdateTokens()));
        public List<StaticColumn> Columns
        {
            get { return (List<StaticColumn>)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        
        void UpdateTokens()
        {
            if (Columns == null)
                return; 

            sp.Children.Clear(); 
            for (int i = 0; i < tokens.Count + 1; i++)
			{
                QueryToken[] subTokens = i == 0? Columns.Select(c=>QueryToken.NewColumn(c)).ToArray():
                                                 tokens[i-1].SubTokens(); 

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
            if(subTokens != null && subTokens.Length != 0)
                sp.Children.Add(new ComboBox
                {
                    Tag = index + 1,
                    ItemsSource = subTokens,
                    SelectedIndex =-1,
                });

            if (tokens.Count <= index)
                tokens.Add(newToken);
            else
            {
                tokens[index] = newToken;
                tokens.RemoveRange(index + 1, tokens.Count - (index + 1)); //all
            }
        }

        public QueryTokenBuilder()
        {
            InitializeComponent();
            sp.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(cb_SelectionChanged));
        }
    }
}
