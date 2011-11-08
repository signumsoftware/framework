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
                ((ChartRequest)e.NewValue).Chart.ChartRequestChanged -= ReDrawChart;

            if (e.NewValue != null)
                ((ChartRequest)e.NewValue).Chart.ChartRequestChanged += ReDrawChart;
        }

        void ChartWindow_Loaded(object sender, RoutedEventArgs e)
        {
            filterBuilder.Filters = FilterOptions = new FreezableCollection<FilterOption>();

            if (Request.Filters != null)
                FilterOptions.AddRange(Request.Filters.Select(f => new FilterOption
                {
                    Token = f.Token,
                    Operation = f.Operation,
                    Value = f.Value
                }));

            ((INotifyCollectionChanged)filterBuilder.Filters).CollectionChanged += Filters_CollectionChanged;
            Request.Chart.ChartRequestChanged += Request_ChartRequestChanged;


            chartBuilder.Description = Description = Navigator.Manager.GetQueryDescription(Request.QueryName);
            Settings = Navigator.GetQuerySettings(Request.QueryName);
            var entityColumn = Description.Columns.SingleOrDefault(a => a.IsEntity);
            EntityType = Reflector.ExtractLite(entityColumn.Type);

            qtbFilters.Token = null;
            qtbFilters.SubTokensEvent += new Func<QueryToken, List<QueryToken>>(qtbFilters_SubTokensEvent);

            SetTitle(); 
        }

        void Request_ChartRequestChanged()
        {
            UpdateMultiplyMessage();
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

        List<QueryToken> qtbFilters_SubTokensEvent(QueryToken arg)
        {
            return QueryUtils.SubTokens(arg, Description.Columns);
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
            UpdateFilters();
            var request = Request;

            if (HasErrors())
                return;

            execute.IsEnabled = false;
            Async.Do(this.FindCurrentWindow(),
                () => resultTable = Server.Return((IChartServer cs) => cs.ExecuteChart(request)),
                () =>
                {
                    request.Chart.NeedNewQuery = false;
                    ReDrawChart();
                },
                () => execute.IsEnabled = true);
        }

        private void UpdateFilters()
        {
            Request.Filters = filterBuilder.Filters.Select(f => f.ToFilter()).ToList();
        }

        private void ReDrawChart()
        {
            if (!Request.Chart.NeedNewQuery)
            {
                if (resultTable != null)
                    SetResults();
            }
        }

        private bool HasErrors()
        {
            GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(Request));

            string errors = Request.FullIntegrityCheck();

            if (string.IsNullOrEmpty(errors))
                return false;

            MessageBox.Show(this.FindCurrentWindow(), "There are errors in the chart settings:\r\n" + errors, "Errors in the chart", MessageBoxButton.OK, MessageBoxImage.Stop);

            return true;
        }

        Func<ResultRow, List<FilterOption>> getFilters;  

        private void SetResults()
        {
            gvResults.Columns.Clear();
            foreach (var t in resultTable.Columns.ZipStrict(Request.ChartTokens()))
            {
                AddListViewColumn(t.Item1, t.Item2);
            }

            if (Request.Chart.GroupResults)
            {
                //so the values don't get affected till next SetResults
                var filters = Request.Filters.Select(f => new FilterOption { Path = f.Token.FullKey(), Value = f.Value, Operation = f.Operation }).ToList();
                var charTokens = Request.ChartTokens().Select(t => new { t.Token, t.Aggregate }).ToArray();

                getFilters =
                    rr => filters.Concat(charTokens.Zip(resultTable.Columns)
                    .Where(t => t.Item1.Aggregate == null)
                    .SelectMany(t => GetTokenFilters(t.Item1.Token, rr[t.Item2]))).ToList();
            }
            else getFilters = null;

            lvResult.ItemsSource = resultTable.Rows;
            if (resultTable.Rows.Length > 0)
            {
                lvResult.SelectedIndex = 0;
                lvResult.ScrollIntoView(resultTable.Rows.First());
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

                if (MessageBox.Show(this.FindCurrentWindow(), message, "Filter Null values?", MessageBoxButton.YesNo, MessageBoxImage.Hand) == MessageBoxResult.Yes)
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

        class ColumnInfo
        {
            public ChartTokenDN ChartToken;
            public ResultColumn Column;
            public ColumnOrderInfo OrderInfo; 
        }

        void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader header = (GridViewColumnHeader)sender;
            ColumnInfo ci = (ColumnInfo)header.Tag;

            if (ci == null)
                return;

            var columnInfos = gvResults.Columns.Select(c => (ColumnInfo)((GridViewColumnHeader)c.Header).Tag).Where(c => c.OrderInfo != null).ToList();

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift || (columnInfos.Count == 1 && columnInfos[0] == ci))
            {

            }
            else
            {
                foreach (var col in columnInfos)
                {
                    col.ChartToken.OrderPriority = null;
                    col.ChartToken.OrderType = null;
                }
            }

            if (ci.OrderInfo != null)
            {
                ci.ChartToken.OrderType = ci.ChartToken.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending; ;
            }
            else
            {
                ci.ChartToken.OrderType = OrderType.Ascending;
                ci.ChartToken.OrderPriority = 1; 
            }

            GenerateChart();
        }

        public void ClearResults()
        {
            resultTable = null;
            lvResult.ItemsSource = null;
            lvResult.Background = Brushes.WhiteSmoke;
        }

        GridViewColumn AddListViewColumn(ResultColumn c, ChartTokenDN ct)
        {
            ChartTokenDN token = new ChartTokenDN();
            
            var columnInfo = new ColumnInfo
            {
                Column = c,
                ChartToken = ct,
            };

            var header = new GridViewColumnHeader
            {
                Content = c.Column.DisplayName,
                Tag = columnInfo
            };

            if (ct.OrderPriority.HasValue)
                columnInfo.OrderInfo = new ColumnOrderInfo(header, ct.OrderType.Value, ct.OrderPriority.Value); 

            GridViewColumn column = new GridViewColumn
            {
                Header = header,
                CellTemplate = CreateDataTemplate(c),
            };
            gvResults.Columns.Add(column);
            return column;
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

            e.Handled = true;
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
