using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using Signum.Entities;
using Signum.Utilities;
using System.Windows.Input;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Windows.Media;
using Signum.Services;
using Signum.Windows.Properties;

namespace Signum.Windows
{
    public partial class CountSearchControl
    {
        public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(CountSearchControl), new UIPropertyMetadata(null));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextZeroItemsProperty =
        DependencyProperty.Register("TextZeroItems", typeof(string), typeof(CountSearchControl), new UIPropertyMetadata(null));
        public string TextZeroItems
        {
            get { return (string)GetValue(TextZeroItemsProperty); }
            set { SetValue(TextZeroItemsProperty, value); }
        }

        private static readonly DependencyProperty FormattedTextProperty =
        DependencyProperty.Register("FormattedText", typeof(string), typeof(CountSearchControl), new UIPropertyMetadata("Total: 0"));
        private string FormattedText
        {
            get { return (string)GetValue(FormattedTextProperty); }
            set { SetValue(FormattedTextProperty, value); }
        }

        public static readonly DependencyProperty QueryNameProperty =
            DependencyProperty.Register("QueryName", typeof(object), typeof(CountSearchControl), new UIPropertyMetadata(null));
        public object QueryName
        {
            get { return (object)GetValue(QueryNameProperty); }
            set { SetValue(QueryNameProperty, value); }
        }

        public static readonly DependencyProperty FilterOptionsProperty =
          DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOption>), typeof(CountSearchControl), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> FilterOptions
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
        }

        public static readonly DependencyProperty ItemsCountProperty =
        DependencyProperty.Register("ItemsCount", typeof(int), typeof(CountSearchControl), new UIPropertyMetadata(0));
        public int ItemsCount
        {
            get { return (int)GetValue(ItemsCountProperty); }
            set { SetValue(ItemsCountProperty, value); }
        }

        public event EventHandler LinkClick; 

        public CountSearchControl()
        {
            this.InitializeComponent();

            FilterOptions = new FreezableCollection<FilterOption>();
            this.Loaded += new RoutedEventHandler(SearchControl_Loaded);
        }

        int queryCount;

        QuerySettings settings;

        void SearchControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SearchControl_Loaded;

            if (DesignerProperties.GetIsInDesignMode(this) || QueryName == null)
                return;

            settings = Navigator.GetQuerySettings(QueryName);

            QueryDescription view = Server.Return((IQueryServer s) => s.GetQueryDescription(QueryName));

            Column entity = view.Columns.SingleOrDefault(a => a.IsEntity);


            foreach (var fo in FilterOptions)
            {
                fo.Column = view.Columns.Where(c => c.Name == fo.ColumnName)
                    .Single(Properties.Resources.Column0NotFoundOnQuery1.Formato(fo.ColumnName, QueryName));
                fo.RefreshRealValue();
            }
            Search();
        }

        public List<Filter> CurrentFilters()
        {
            return FilterOptions.Select(f => f.ToFilter()).ToList();
        }

        public void Search()
        {
            queryCount = 0;

            object vn = QueryName;
            var lf = CurrentFilters();

            Async.Do(this.FindCurrentWindow(),
                () =>

                    queryCount = Server.Return((IQueryServer s)=>s.GetQueryCount(vn, lf)),
                () =>
                {
                    ItemsCount = queryCount;
                    if (ItemsCount == 0)
                    {
                        FormattedText = (TextZeroItems ?? Properties.Resources.ThereIsNo0)
                            .Formato(QueryUtils.GetNiceQueryName(QueryName));
                        tb.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        FormattedText = (Text ?? "{1}: {0}")
                            .Formato(ItemsCount, QueryUtils.GetNiceQueryName(QueryName));
                    }
                },
                () => { });
        }

        private void ItemCount_Click(object sender, MouseButtonEventArgs e)
        {
            if (LinkClick != null)
                LinkClick(this, EventArgs.Empty);
            else
            {
                Navigator.Find(new FindOptions
                {
                    FilterOptions = FilterOptions.ToList(),
                    QueryName = QueryName
                });
            }
        }
    }
}
