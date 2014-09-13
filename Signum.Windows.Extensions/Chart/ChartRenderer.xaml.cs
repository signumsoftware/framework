using Signum.Entities.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
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
using Signum.Windows.DynamicQuery;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Signum.Entities.UserQueries;
using System.Collections.ObjectModel;
using Signum.Services;
using System.Threading;
using Signum.Entities.UserAssets;

namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartRenderer.xaml
    /// </summary>
    public partial class ChartRenderer : UserControl
    {
        public static readonly DependencyProperty OrderOptionsProperty =
            DependencyProperty.Register("OrderOptions", typeof(ObservableCollection<OrderOption>), typeof(ChartRenderer), new UIPropertyMetadata(null));
        public ObservableCollection<OrderOption> OrderOptions
        {
            get { return (ObservableCollection<OrderOption>)GetValue(OrderOptionsProperty); }
            set { SetValue(OrderOptionsProperty, value); }
        }

        public static readonly DependencyProperty FilterOptionsProperty =
         DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOption>), typeof(ChartRenderer), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> FilterOptions
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
        }

        public bool GenerateOnLoad { get; set; }

        public QueryDescription Description;

        public QuerySettings Settings { get; private set; }

        public ResultTable ResultTable { get; set; }

        public ChartRequest Request
        {
            get { return (ChartRequest)DataContext; }
        }

        public ChartRenderer()
        {
            InitializeComponent();

            OrderOptions = new ObservableCollection<OrderOption>();


            webBrowser.HideScriptErrors(true);
            this.Loaded += ChartRenderer_Loaded;
            this.DataContextChanged += ChartRenderer_DataContextChanged;
        }

        void ChartRenderer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(DataContext is ChartRequest))
                return;

            Settings = Finder.GetQuerySettings(Request.QueryName);
            Description = DynamicQueryServer.GetQueryDescription(Request.QueryName);
        }

        void ChartRenderer_Loaded(object sender, RoutedEventArgs e)
        {
            webBrowser.ObjectForScripting = new ScriptInterface { renderer = this };
            webBrowser.NavigateToString(FullHtml.Value);
            webBrowser.LoadCompleted += webBrowser_LoadCompleted;
            if (GenerateOnLoad)
                OnGenerateChart();
        }

        bool webBrowserLoaded;
        void webBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            webBrowserLoaded = true;
        }

        static string baseResourcePath = "Signum.Windows.Extensions.Chart.Html.";

        static ResetLazy<string> FullHtml = new ResetLazy<string>(() =>
        {
            string baseHtml = typeof(ChartRequestWindow).Assembly.ReadResourceStream(baseResourcePath + "ChartContainer.htm");

            return Regex.Replace(baseHtml, @"\<(?<tag>style|script) src=""(?<fileName>.*?)"" \/\>", m =>
                (m.Groups["tag"].Value == "style" ? "<style type=\"text/css\">\r\n" : "<script>\r\n") +
                 typeof(ChartRequestWindow).Assembly.ReadResourceStream(baseResourcePath + m.Groups["fileName"].Value) +
                 (m.Groups["tag"].Value == "style" ? "\r\n</style>" : "\r\n</script>"));
        });

        internal void ReDrawChart()
        {
            if (!Request.NeedNewQuery)
            {
                if (ResultTable != null)
                    SetResults();
            }
            else
            {
                ClearResults();
            }
        }

        internal void SetResults(string script = null)
        {
            if (ResultTable == null)
                return;

            if (script == null)
                script = Request.ChartScript.Script;

            FillGridView();

            if (Request.GroupResults)
            {
                //so the values don't get affected till next SetResults
                var filters = Request.Filters.Select(f => new FilterOption { ColumnName = f.Token.FullKey(), Value = f.Value, Operation = f.Operation }).ToList();
                var keyColunns = Request.Columns
                    .Zip(ResultTable.Columns, (t, c) => new { t.Token, Column = c })
                    .Where(a => !(a.Token.Token is AggregateToken)).ToArray();

                lastRequest = new LastRequest
                {
                    KeyColumns = Request.Columns.Iterate()
                    .Where(a => a.Value.ScriptColumn.IsGroupKey)
                    .Select(a => new KeyColumn { Position = a.Position, Token = a.Value.Token.Try(t=>t.Token) })
                    .ToList(),
                    Filters = Request.Filters.Where(a => !(a.Token is AggregateToken)).Select(f => new FilterOption
                    {
                        Token = f.Token,
                        Operation = f.Operation,
                        Value = f.Value,
                    }).ToList(),
                    GroupResults = Request.GroupResults,
                    GetFilter = rr => keyColunns.Select(t => GetTokenFilters(t.Token.Token, rr[t.Column])).ToList()
                };
            }
            else lastRequest = new LastRequest { GroupResults = false };

            lvResult.ItemsSource = ResultTable.Rows;
            if (ResultTable.Rows.Length > 0)
            {
                lvResult.SelectedIndex = 0;
                lvResult.ScrollIntoView(ResultTable.Rows.FirstEx());
            }

            lvResult.Background = Brushes.White;

            var jsonData = ChartUtils.DataJson(Request, ResultTable);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);

            if (webBrowserLoaded)
                WebBroserInvoke(script, json);
            else
                webBrowser.LoadCompleted += (s, e) => WebBroserInvoke(script, json);
           
        }

        private void WebBroserInvoke(string script, string json)
        {
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

        static FilterOption GetTokenFilters(QueryToken queryToken, object p)
        {
            return new FilterOption(queryToken.FullKey(), p);
        }

        public void ClearResults()
        {
            ResultTable = null;
            gvResults.Columns.Clear();
            lvResult.ItemsSource = null;
            lvResult.Background = Brushes.WhiteSmoke;

            lastRequest = null;

            var keys = Request.Columns.Select(a => a.Token.Try(t => t.Token)).Where(a => a != null && !(a is AggregateToken)).Select(a => a.FullKey()).ToHashSet();
            OrderOptions.RemoveAll(a => !(a.Token is AggregateToken) && !keys.Contains(a.Token.FullKey()));
        }

        void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            SortGridViewColumnHeader header = sender as SortGridViewColumnHeader;

            if (header == null)
                return;

            string canOrder = QueryUtils.CanOrder(header.RequestColumn.Token);
            if (canOrder.HasText())
            {
                MessageBox.Show(Window.GetWindow(this), canOrder);
                return;
            }

            header.ChangeOrders(OrderOptions);

            OnGenerateChart();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnGenerateChart();
        }

        public void OnGenerateChart()
        {
            if (GenerateChart == null)
                throw new InvalidOperationException("GenerateChart not set");

            GenerateChart();
        }

        public event Action GenerateChart;

        private void FillGridView()
        {
            gvResults.Columns.Clear();
            foreach (var rc in ResultTable.Columns)
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
                Lite<IdentifiableEntity> lite = row.Entity;

                if (Navigator.IsNavigable(lite.EntityType, isSearch: true))
                    Navigator.NavigateUntyped(lite);
            }
            else
            {
                var subFilters = lastRequest.GetFilter(row);

                Finder.Explore(new ExploreOptions(Request.QueryName)
                {
                    FilterOptions = lastRequest.Filters.Concat(subFilters).ToList(),
                    SearchOnLoad = true,
                });
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


        [ComVisible(true)]
        public class ScriptInterface
        {
            internal ChartRenderer renderer;

            public void OpenSubgroup(string dataClicks)
            {
                renderer.OpenSubgroup(dataClicks);
            }
        }
        LastRequest lastRequest;

        internal void OpenSubgroup(string dataClicks)
        {
            var dic = dataClicks.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries).ToDictionary(a => a.Split('=')[0], a => a.Split('=')[1]);

            if (!lastRequest.GroupResults)
            {
                Lite<IdentifiableEntity> lite = (Lite<IdentifiableEntity>)FilterValueConverter.Parse(dic["entity"], this.Description.Columns.Single(a => a.IsEntity).Type, isList: false);

                if (Navigator.IsNavigable(lite.EntityType, isSearch: true))
                    Navigator.NavigateUntyped(lite, new NavigateOptions());
            }
            else
            {
                var subFilters = lastRequest.KeyColumns.Select(t => 
                    new FilterOption(t.Token.FullKey(), FilterValueConverter.Parse(dic["c" + t.Position], t.Token.Type, isList: false)));

                Finder.Explore(new ExploreOptions(Request.QueryName)
                {
                    FilterOptions = lastRequest.Filters.Concat(subFilters).ToList(),
                    SearchOnLoad = true,
                });
            }
        }


        internal void ShowData()
        {
            tabData.IsSelected = true;
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
