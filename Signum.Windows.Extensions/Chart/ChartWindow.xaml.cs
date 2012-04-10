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
using Signum.Entities;
using Signum.Entities.Chart;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Services;
using Signum.Entities.Reflection;
using Signum.Windows.DynamicQuery;
using Signum.Utilities.DataStructures;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window
    {
        public static readonly DependencyProperty FilterOptionsProperty =
         DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOption>), typeof(ChartWindow), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> FilterOptions
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
        }

        public static readonly DependencyProperty OrderOptionsProperty =
         DependencyProperty.Register("OrderOptions", typeof(ObservableCollection<OrderOption>), typeof(ChartWindow), new UIPropertyMetadata(null));
        public ObservableCollection<OrderOption> OrderOptions
        {
            get { return (ObservableCollection<OrderOption>)GetValue(OrderOptionsProperty); }
            set { SetValue(OrderOptionsProperty, value); }
        }

        public ResultTable resultTable;
        public QueryDescription Description;
        public QuerySettings Settings { get; private set; }
        public Type EntityType;

        ChartRendererBase chartRenderer; 

        public ChartRequest Request
        {
            get { return (ChartRequest)DataContext; }
        }

        public ChartWindow()
        {
            InitializeComponent();

            chartRenderer = ChartClient.GetChartRenderer();
            rendererPlaceHolder.Children.Insert(0, chartRenderer); 

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ChartBuilder_DataContextChanged);
            this.Loaded += new RoutedEventHandler(ChartWindow_Loaded);

            userChartMenuItem.ChartWindow = this;
        }

        void ChartBuilder_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
                ((ChartRequest)e.NewValue).Chart.ChartRequestChanged -= Request_ChartRequestChanged;

            if (e.NewValue != null)
                ((ChartRequest)e.NewValue).Chart.ChartRequestChanged += Request_ChartRequestChanged;

            qtbFilters.UpdateTokenList();
        }

        void ChartWindow_Loaded(object sender, RoutedEventArgs e)
        {
            filterBuilder.Filters = FilterOptions = new FreezableCollection<FilterOption>();
            OrderOptions = new ObservableCollection<OrderOption>();

            UpdateFiltersOrdersUserInterface();

            ((INotifyCollectionChanged)filterBuilder.Filters).CollectionChanged += Filters_CollectionChanged;
            Request.Chart.ChartRequestChanged += Request_ChartRequestChanged;

            chartBuilder.Description = Description = Navigator.Manager.GetQueryDescription(Request.QueryName);
            Settings = Navigator.GetQuerySettings(Request.QueryName);
            var entityColumn = Description.Columns.SingleOrDefaultEx(a => a.IsEntity);
            EntityType = Reflector.ExtractLite(entityColumn.Type);

            qtbFilters.Token = null;
            qtbFilters.SubTokensEvent += new Func<QueryToken, List<QueryToken>>(qtbFilters_SubTokensEvent);

            SetTitle(); 
        }

        internal void UpdateFiltersOrdersUserInterface()
        {
            FilterOptions.Clear();
            if (Request.Filters != null)
                FilterOptions.AddRange(Request.Filters.Select(f => new FilterOption
                {
                    Token = f.Token,
                    Operation = f.Operation,
                    Value = f.Value
                }));

            OrderOptions.Clear();
            if (Request.Orders != null)
                OrderOptions.AddRange(Request.Orders.Select(o => new OrderOption
                {
                    Token = o.Token,
                    OrderType = o.OrderType,
                }));
        }

        void Request_ChartRequestChanged()
        {
            UpdateMultiplyMessage();
            ReDrawChart(); 
        }

        void Filters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMultiplyMessage();
        }

        void SetTitle()
        {
            tbEntityType.Text = EntityType.NicePluralName();

            string niceQueryName = QueryUtils.GetNiceName(Request.QueryName);

            if (niceQueryName.StartsWith(tbEntityType.Text))
                niceQueryName = niceQueryName.Substring(tbEntityType.Text.Length).Trim();
            else
                niceQueryName = "- " + niceQueryName;

            tbQueryName.Text = niceQueryName;
        }

        public void UpdateMultiplyMessage()
        {
            string message = CollectionElementToken.MultipliedMessage(Request.Multiplications, EntityType);

            tbMultiplications.Text = message;
            brMultiplications.Visibility = message.HasText() ? Visibility.Visible : Visibility.Collapsed;
        }

        List<QueryToken> qtbFilters_SubTokensEvent(QueryToken token)
        {
            var cr = (ChartRequest)DataContext;
            if (cr == null || Description == null)
                return new List<QueryToken>();

            return cr.Chart.SubTokensFilters(token, Description.Columns);
        }

        private void btCreateFilter_Click(object sender, RoutedEventArgs e)
        {
            filterBuilder.AddFilter(qtbFilters.Token);
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            GenerateChart();
        }

        private void execute_Click(object sender, RoutedEventArgs e)
        {
            GenerateChart();
        }

        public void GenerateChart()
        {
            Request.Filters = filterBuilder.Filters.Select(f => f.ToFilter()).ToList();
            Request.Orders = OrderOptions.Select(o => o.ToOrder()).ToList();

            var request = Request;

            if (HasErrors())
                return;

            execute.IsEnabled = false;
            Async.Do(
                () => resultTable = Server.Return((IChartServer cs) => cs.ExecuteChart(request)),
                () =>
                {
                    request.Chart.NeedNewQuery = false;
                    ReDrawChart();
                },
                () => execute.IsEnabled = true);
        }

        private void ReDrawChart()
        {
            if (!Request.Chart.NeedNewQuery)
            {
                if (resultTable != null)
                    SetResults();
            }
            else
            {    
                ClearResults();
            }
        }

        private bool HasErrors()
        {
            GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(Request));

            string errors = Request.FullIntegrityCheck();

            if (string.IsNullOrEmpty(errors))
                return false;

            MessageBox.Show(Window.GetWindow(this), "There are errors in the chart settings:\r\n" + errors, "Errors in the chart", MessageBoxButton.OK, MessageBoxImage.Stop);

            return true;
        }

        Func<ResultRow, List<FilterOption>> getFilters;  

        private void SetResults()
        {
            FillGridView();

            if (Request.Chart.GroupResults)
            {
                //so the values don't get affected till next SetResults
                var filters = Request.Filters.Select(f => new FilterOption { Path = f.Token.FullKey(), Value = f.Value, Operation = f.Operation }).ToList();
                var keyColunns = Request.ChartTokens()
                    .Zip(resultTable.Columns, (t, c) => new { t.Token, Column = c })
                    .Where(a => !(a.Token is AggregateToken)).ToArray();

                getFilters =
                    rr => keyColunns.SelectMany(t => GetTokenFilters(t.Token, rr[t.Column])).ToList();
            }
            else getFilters = null;

            lvResult.ItemsSource = resultTable.Rows;
            if (resultTable.Rows.Length > 0)
            {
                lvResult.SelectedIndex = 0;
                lvResult.ScrollIntoView(resultTable.Rows.FirstEx());
            }

            lvResult.Background = Brushes.White;

            chartRenderer.ChartRequest = Request;
            chartRenderer.ResultTable = resultTable;

            try
            {
                chartRenderer.DrawChart();
            }
            catch (ChartNullException ex)
            {
                ChartRequest cr = Request;

                ChartTokenDN ct = cr.Chart.GetToken(ex.ChartTokenName);

                string message = "There are null values in {0} ({1}). \r\n Filter values?".Formato(ct.Token.ToString(), ct.PropertyLabel);

                if (MessageBox.Show(Window.GetWindow(this), message, "Filter Null values?", MessageBoxButton.YesNo, MessageBoxImage.Hand) == MessageBoxResult.Yes)
                {
                    QueryToken token = ct.Token;

                    if (token is IntervalQueryToken || token is NetPropertyToken)
                        token = token.Parent;

                    filterBuilder.Filters.Add(new FilterOption { Token = token, Operation = FilterOperation.DistinctTo, RealValue = null });
                }
            }
        }

        static FilterOption[] GetTokenFilters(QueryToken queryToken, object p)
        {
            if (queryToken is IntervalQueryToken)
            {
                var filters = miGetIntervalFilters.GetInvoker(queryToken.Type.GetGenericArguments())(queryToken.Parent, p);

                return filters.ToArray();
            }
            else
                return new[] { new FilterOption(queryToken.FullKey(), p) };
        }

        static GenericInvoker<Func<QueryToken, object, IEnumerable<FilterOption>>> miGetIntervalFilters = new GenericInvoker<Func<QueryToken, object, IEnumerable<FilterOption>>>(
            (qt, obj) => GetIntervalFilters<int>(qt, (NullableInterval<int>)obj));
        static IEnumerable<FilterOption> GetIntervalFilters<T>(QueryToken queryToken, NullableInterval<T> interval)
            where T: struct, IComparable<T>, IEquatable<T>
        {
            if (interval.Min.HasValue)
                yield return new FilterOption { Path = queryToken.FullKey(), Value = interval.Min.Value, Operation = FilterOperation.GreaterThanOrEqual };

            if (interval.Max.HasValue)
                yield return new FilterOption { Path = queryToken.FullKey(), Value = interval.Max.Value, Operation = FilterOperation.LessThan };
        }

        void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            SortGridViewColumnHeader header = sender as SortGridViewColumnHeader;

            if (header == null)
                return;

            string canOrder = QueryUtils.CanOrder(header.RequestColumn.Token);
            if (canOrder.HasText())
            {
                MessageBox.Show(canOrder);
                return;
            }

            header.ChangeOrders(OrderOptions);         

            GenerateChart();
        }

        public void ClearResults()
        {
            resultTable = null;
            gvResults.Columns.Clear();
            lvResult.ItemsSource = null;
            lvResult.Background = Brushes.WhiteSmoke;

            var keys = Request.ChartTokens().Select(a => a.Token).Where(a => a != null && !(a is AggregateToken)).Select(a => a.FullKey()).ToHashSet();
            OrderOptions.RemoveAll(a => !(a.Token is AggregateToken) && !keys.Contains(a.Token.FullKey()));
        }

        private void FillGridView()
        {
            gvResults.Columns.Clear();
            foreach (var rc in resultTable.Columns)
            {
                gvResults.Columns.Add(new GridViewColumn
                {
                    Header = new SortGridViewColumnHeader
                    {
                        Content = rc.Column.DisplayName,
                        RequestColumn = rc.Column,
                    },
                    CellTemplate = CreateDataTemplate(rc),
                });
            }

            SortGridViewColumnHeader.SetColumnAdorners(gvResults, OrderOptions);
        }

        DataTemplate CreateDataTemplate(ResultColumn c)
        {
            Binding b = new Binding("[{0}]".Formato(c.Index)) { Mode = BindingMode.OneTime };
            DataTemplate dt = Settings.GetFormatter(c.Column)(b);
            return dt;
        }

        void lvResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ResultRow row = (ResultRow)lvResult.SelectedItem;

            if (row == null)
                return;

            ShowRow(row);

            e.Handled = true;
        }

        public void ShowRow(ResultRow row)
        {
            if (row.Table.HasEntities)
            {
                IdentifiableEntity entity = (IdentifiableEntity)Server.Convert(row.Entity, EntityType);

                if (Navigator.IsViewable(EntityType, false))
                    Navigator.NavigateUntyped(entity, new NavigateOptions { Admin = false });
            }
            else
            {
                Navigator.Explore(new ExploreOptions(Request.QueryName)
                {
                    FilterOptions = getFilters(row),
                    SearchOnLoad = true,
                });
            }
        }
    }

    [Serializable]
    public class ChartNullException : Exception
    {
        public ChartTokenName ChartTokenName { get; private set; }

        public override string Message
        {
            get { return "There are null values in {0}".Formato(ChartTokenName); }
        }

        public ChartNullException(ChartTokenName name) { this.ChartTokenName = name; }
    }

    public class ChartRendererBase : UserControl
    {
        public virtual void DrawChart() { }
        public ResultTable ResultTable { get; set; }
        public ChartRequest ChartRequest { get; set; }
    }
}
