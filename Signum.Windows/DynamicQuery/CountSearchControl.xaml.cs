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
using System.Windows.Automation;

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

        public static readonly DependencyProperty TextWaitingProperty =
            DependencyProperty.Register("TextWaiting", typeof(string), typeof(CountSearchControl), new UIPropertyMetadata(null));
        public string TextWaiting
        {
            get { return (string)GetValue(TextWaitingProperty); }
            set { SetValue(TextWaitingProperty, value); }
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



        public static readonly DependencyProperty OrderOptionsProperty =
            DependencyProperty.Register("OrderOptions", typeof(ObservableCollection<OrderOption>), typeof(CountSearchControl), new UIPropertyMetadata(null));
        public ObservableCollection<OrderOption> OrderOptions
        {
            get { return (ObservableCollection<OrderOption>)GetValue(OrderOptionsProperty); }
            set { SetValue(OrderOptionsProperty, value); }
        }

        public static readonly DependencyProperty FilterOptionsProperty =
            DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOption>), typeof(CountSearchControl), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> FilterOptions
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
        }


        public static readonly DependencyProperty ColumnOptionsModeProperty =
            DependencyProperty.Register("ColumnOptionsMode", typeof(ColumnOptionsMode), typeof(CountSearchControl), new UIPropertyMetadata(ColumnOptionsMode.Add));
        public ColumnOptionsMode ColumnOptionsMode
        {
            get { return (ColumnOptionsMode)GetValue(ColumnOptionsModeProperty); }
            set { SetValue(ColumnOptionsModeProperty, value); }
        }

        public static readonly DependencyProperty ColumnsOptionsProperty =
            DependencyProperty.Register("ColumnOptions", typeof(ObservableCollection<ColumnOption>), typeof(CountSearchControl), new UIPropertyMetadata(null));
        public ObservableCollection<ColumnOption> ColumnOptions
        {
            get { return (ObservableCollection<ColumnOption>)GetValue(ColumnsOptionsProperty); }
            set { SetValue(ColumnsOptionsProperty, value); }
        }


        public static readonly DependencyProperty ItemsCountProperty =
            DependencyProperty.Register("ItemsCount", typeof(int), typeof(CountSearchControl), new UIPropertyMetadata(0));
        public int ItemsCount
        {
            get { return (int)GetValue(ItemsCountProperty); }
            set { SetValue(ItemsCountProperty, value); }
        }

        public static readonly DependencyProperty IsSearchingProperty =
           DependencyProperty.Register("IsSearching", typeof(bool), typeof(CountSearchControl), new PropertyMetadata(false));
        public bool IsSearching
        {
            get { return (bool)GetValue(IsSearchingProperty); }
            set { SetValue(IsSearchingProperty, value); }
        }

        public static readonly DependencyProperty FilterColumnProperty =
            DependencyProperty.Register("FilterColumn", typeof(string), typeof(CountSearchControl), new UIPropertyMetadata(null,
          (d, e) => ((CountSearchControl)d).AssetNotLoaded(e)));
        public string FilterColumn
        {
            get { return (string)GetValue(FilterColumnProperty); }
            set { SetValue(FilterColumnProperty, value); }
        }

        public static readonly DependencyProperty FilterRouteProperty =
            DependencyProperty.Register("FilterRoute", typeof(string), typeof(CountSearchControl), new UIPropertyMetadata(null,
                (d, e) => ((CountSearchControl)d).AssetNotLoaded(e)));
        public string FilterRoute
        {
            get { return (string)GetValue(FilterRouteProperty); }
            set { SetValue(FilterRouteProperty, value); }
        }

        private void AssetNotLoaded(DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded)
                throw new InvalidProgramException("You can not change {0} property once loaded".FormatWith(e.Property));
        }

        public event EventHandler LinkClick; 

        public CountSearchControl()
        {
            this.InitializeComponent();

            FilterOptions = new FreezableCollection<FilterOption>();
            ColumnOptions = new ObservableCollection<ColumnOption>();
            OrderOptions = new ObservableCollection<OrderOption>();
            this.Loaded += new RoutedEventHandler(SearchControl_Loaded);
           
            this.Bind(AutomationProperties.ItemStatusProperty, this, "ItemsCount"); 
        }


        void item_BindingValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsLoaded && e.NewValue != null)
            {
                Search();
            }
        }

        QueryDescription qd;

        void SearchControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SearchControl_Loaded;

            if (DesignerProperties.GetIsInDesignMode(this) || QueryName == null)
                return;

            if (qd == null)
                qd = DynamicQueryServer.GetQueryDescription(QueryName);

            if (FilterColumn.HasText())
            {
                FilterOptions.Add(new FilterOption
                {
                    ColumnName = FilterColumn,
                    Operation = FilterOperation.EqualTo,
                    Frozen = true,
                }.Bind(FilterOption.ValueProperty, new Binding("DataContext" + (FilterRoute.HasText() ? "." + FilterRoute : null)) { Source = this }));
                ColumnOptions.Add(new ColumnOption(FilterColumn));
                ColumnOptionsMode = ColumnOptionsMode.Remove;
            }

            FilterOption.SetFilterTokens(FilterOptions, qd);

            AutomationProperties.SetName(this, QueryUtils.GetKey(QueryName));

            Search();

            foreach (var item in FilterOptions)
            {
                item.BindingValueChanged += new DependencyPropertyChangedEventHandler(item_BindingValueChanged);
            }
        }   
        
        
        bool searchQueued;

        public void Search()
        {
            if (IsSearching)
            {
                searchQueued = true;
                return;
            }

            FormattedText = (TextWaiting ?? QueryUtils.GetNiceName(QueryName) + "...");
            tb.FontWeight = FontWeights.Regular;

            var options = new QueryCountOptions
            {
                QueryName = QueryName,
                FilterOptions = FilterOptions.ToList()
            };

            DynamicQueryServer.QueryCountBatch(options, count =>
            {
                ItemsCount = count;
                if (ItemsCount == 0)
                {
                    FormattedText = (TextZeroItems ?? SearchMessage.ThereIsNo0.NiceToString()).FormatWith(QueryUtils.GetNiceName(QueryName));
                    tb.FontWeight = FontWeights.Regular;
                }
                else
                {
                    FormattedText = (Text ?? (QueryUtils.GetNiceName(QueryName) + ": {0}")).FormatWith(ItemsCount);
                    tb.FontWeight = FontWeights.Bold;
                }

                if (searchQueued)
                {
                    searchQueued = false;
                    Search();
                }
            },
            () => { });
        }


        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (LinkClick != null)
                LinkClick(this, EventArgs.Empty);
            else
                DefaultClick();

            e.Handled = true;
        }

        public void DefaultClick()
        {
            Finder.Explore(new ExploreOptions(QueryName)
            {
                OrderOptions = OrderOptions.ToList(),
                FilterOptions = FilterOptions.ToList(),
                ColumnOptions = ColumnOptions.ToList(),
                ColumnOptionsMode = ColumnOptionsMode,
                SearchOnLoad = true,
            });
        }

        public void Reinitialize(IEnumerable<FilterOption> filters, List<ColumnOption> columns, ColumnOptionsMode columnOptionsMode, List<OrderOption> orders)
        {
            if (qd == null)
                qd = DynamicQueryServer.GetQueryDescription(QueryName);

            ColumnOptions.Clear();
            ColumnOptions.AddRange(columns);
            ColumnOptionsMode = columnOptionsMode;
           
            FilterOptions.Clear();
            FilterOptions.AddRange(filters);
            FilterOption.SetFilterTokens(FilterOptions, qd);

            OrderOptions.Clear();
            OrderOptions.AddRange(orders);
            OrderOption.SetOrderTokens(OrderOptions, qd);
        }
    }
}
