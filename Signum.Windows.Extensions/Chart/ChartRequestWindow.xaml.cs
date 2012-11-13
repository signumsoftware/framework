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
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
using Signum.Entities.UserQueries;

namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartWindow.xaml
    /// </summary>
    public partial class ChartRequestWindow : Window
    {
        public static readonly DependencyProperty FilterOptionsProperty =
         DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOption>), typeof(ChartRequestWindow), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> FilterOptions
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
        }

        public static readonly DependencyProperty OrderOptionsProperty =
         DependencyProperty.Register("OrderOptions", typeof(ObservableCollection<OrderOption>), typeof(ChartRequestWindow), new UIPropertyMetadata(null));
        public ObservableCollection<OrderOption> OrderOptions
        {
            get { return (ObservableCollection<OrderOption>)GetValue(OrderOptionsProperty); }
            set { SetValue(OrderOptionsProperty, value); }
        }

        public ResultTable resultTable;
        public QueryDescription Description;
        public QuerySettings Settings { get; private set; }
        public Type EntityType;


        public ChartRequest Request
        {
            get { return (ChartRequest)DataContext; }
        }

        public ChartRequestWindow()
        {
            InitializeComponent();

            //rendererPlaceHolder.Children.Insert(0, chartRenderer); 

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ChartBuilder_DataContextChanged);
            this.Loaded += new RoutedEventHandler(ChartWindow_Loaded);
            webBrowser.HideScriptErrors(true);

            userChartMenuItem.ChartWindow = this;
        }

        void ChartBuilder_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
                ((ChartRequest)e.NewValue).ChartRequestChanged -= Request_ChartRequestChanged;

            if (e.NewValue != null)
                ((ChartRequest)e.NewValue).ChartRequestChanged += Request_ChartRequestChanged;

            qtbFilters.UpdateTokenList();
        }

        void ChartWindow_Loaded(object sender, RoutedEventArgs e)
        {
            filterBuilder.Filters = FilterOptions = new FreezableCollection<FilterOption>();
            OrderOptions = new ObservableCollection<OrderOption>();

            UpdateFiltersOrdersUserInterface();

            ((INotifyCollectionChanged)filterBuilder.Filters).CollectionChanged += Filters_CollectionChanged;
            Request.ChartRequestChanged += Request_ChartRequestChanged;

            chartBuilder.Description = Description = Navigator.Manager.GetQueryDescription(Request.QueryName);
            Settings = Navigator.GetQuerySettings(Request.QueryName);
            var entityColumn = Description.Columns.SingleOrDefaultEx(a => a.IsEntity);
            EntityType = Lite.Extract(entityColumn.Type);

            qtbFilters.Token = null;
            qtbFilters.SubTokensEvent += new Func<QueryToken, List<QueryToken>>(qtbFilters_SubTokensEvent);

            SetTitle();

            webBrowser.ObjectForScripting = new ScriptInterface { window = this };
            webBrowser.NavigateToString(FullHtml.Value);
        }


        static string baseResourcePath = "Signum.Windows.Extensions.Chart.Html.";

        static ResetLazy<string> FullHtml = new ResetLazy<string>(() =>
        {
            string baseHtml = typeof(ChartRequestWindow).Assembly.ReadResourceStream(baseResourcePath + "ChartContainer.htm");

            return Regex.Replace(baseHtml, @"\<(?<tag>style|script) src=""(?<fileName>.*?)"" \/\>", m =>
                (m.Groups["tag"].Value == "style" ? "<style type=\"text/css\">\r\n" : "<script>\r\n" )+ 
                 typeof(ChartRequestWindow).Assembly.ReadResourceStream(baseResourcePath + m.Groups["fileName"].Value) +
                 (m.Groups["tag"].Value == "style" ? "\r\n</style>" : "\r\n</script>"));
        }); 

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

            return ChartUtils.SubTokensChart(token, Description.Columns, cr.GroupResults);
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
                    request.NeedNewQuery = false;
                    ReDrawChart();
                },
                () => execute.IsEnabled = true);
        }

        private void ReDrawChart()
        {
            if (!Request.NeedNewQuery)
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


        internal void SetResults(string script = null)
        {
            if (resultTable == null)
                return;

            if(script == null)
                script = Request.ChartScript.Script; 

            FillGridView();

            if (Request.GroupResults)
            {
                //so the values don't get affected till next SetResults
                var filters = Request.Filters.Select(f => new FilterOption { Path = f.Token.FullKey(), Value = f.Value, Operation = f.Operation }).ToList();
                var keyColunns = Request.Columns
                    .Zip(resultTable.Columns, (t, c) => new { t.Token, Column = c })
                    .Where(a => !(a.Token is AggregateToken)).ToArray();

                lastRequest = new LastRequest
                {
                    KeyColumns = Request.Columns.Iterate()
                    .Where(a => a.Value.ScriptColumn.IsGroupKey)
                    .Select(a => new KeyColumn { Position = a.Position, Token = a.Value.Token })
                    .ToList(),
                    Filters = Request.Filters.Where(a => !(a.Token is AggregateToken)).Select(f => new FilterOption
                    {
                        Token = f.Token,
                        Value = f.Value,
                        Operation = f.Operation
                    }).ToList(),
                    GroupResults = Request.GroupResults,
                    GetFilter = rr => keyColunns.Select(t => GetTokenFilters(t.Token, rr[t.Column])).ToList()
                };
            }
            else lastRequest = new LastRequest { GroupResults = false };

            lvResult.ItemsSource = resultTable.Rows;
            if (resultTable.Rows.Length > 0)
            {
                lvResult.SelectedIndex = 0;
                lvResult.ScrollIntoView(resultTable.Rows.FirstEx());
            }

            lvResult.Background = Brushes.White;

            var jsonData = ChartUtils.DataJson(Request, resultTable);

            var json = new JavaScriptSerializer().Serialize(jsonData);

            
            try
            {
                webBrowser.InvokeScript("reDraw", script, json);
            }
            catch (Exception e)
            {
                errorLine.Text = e.Message;
                errorLine.Visibility = System.Windows.Visibility.Visible;
            }

            errorLine.Text = null;
            errorLine.Visibility = System.Windows.Visibility.Collapsed;
        }

        static FilterOption GetTokenFilters(QueryToken queryToken, object p)
        {
            return new FilterOption(queryToken.FullKey(), p);
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
                MessageBox.Show(this, canOrder);
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

            lastRequest = null;

            var keys = Request.Columns.Select(a => a.Token).Where(a => a != null && !(a is AggregateToken)).Select(a => a.FullKey()).ToHashSet();
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

       

        ChartScript chartScriptControl;

        private void edit_Click(object sender, RoutedEventArgs e)
        {
            chartScriptControl = new ChartScript();
            chartScriptControl.RequestWindow = this;

            Navigator.Navigate(Request.ChartScript, new NavigateOptions()
            {
                View = chartScriptControl,
                Clone = false,
            }); 
        }

        [ComVisible(true)]
        public class ScriptInterface
        {
            internal ChartRequestWindow window;

            public void OpenSubgroup(string dataClicks)
            {
                window.OpenSubgroup(dataClicks);
            }
        }

        class LastRequest
        {
            public List<FilterOption> Filters;

            public bool GroupResults;

            public List<KeyColumn> KeyColumns; 

            public Func<ResultRow, List<FilterOption>> GetFilter; 
        }

        class KeyColumn
        {
            public int Position;
            public QueryToken Token;
        }

        LastRequest lastRequest;

        internal void OpenSubgroup(string dataClicks)
        {
            var dic = dataClicks.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries).ToDictionary(a => a.Split('=')[0], a => a.Split('=')[1]);

            if (!lastRequest.GroupResults)
            {
                Lite lite = (Lite)FilterValueConverter.Parse(dic["entity"], this.Description.Columns.Single(a => a.IsEntity).Type);

                if (Navigator.IsNavigable(lite.RuntimeType, isSearchEntity: true))
                    Navigator.NavigateUntyped(lite, new NavigateOptions());
            }
            else
            {
                var subFilters = lastRequest.KeyColumns.Select(t=>new FilterOption(t.Token.FullKey(), FilterValueConverter.Parse(dic["c" + t.Position], t.Token.Type)));

                Navigator.Explore(new ExploreOptions(Request.QueryName)
                {
                    FilterOptions = lastRequest.Filters.Concat(subFilters).ToList(),
                    SearchOnLoad = true,
                }); 
            }
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
                Lite lite = row.Entity;

                if (Navigator.IsNavigable(lite.RuntimeType, isSearchEntity: true))
                    Navigator.NavigateUntyped(lite);
            }
            else
            {
                var subFilters = lastRequest.GetFilter(row);

                Navigator.Explore(new ExploreOptions(Request.QueryName)
                {
                    FilterOptions = lastRequest.Filters.Concat(subFilters).ToList(),
                    SearchOnLoad = true,
                });
            }
        }
    }

    public static class WebBrowserHacks
    {
        public static void HideScriptErrors(this WebBrowser wb, bool hide)
        {
            wb.Navigated += (s, args) =>
            {
                FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fiComWebBrowser == null) return;
                object objComWebBrowser = fiComWebBrowser.GetValue(wb);
                if (objComWebBrowser == null) return;
                objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
            };
        }
    }
}
