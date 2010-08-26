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
        public Type EntityType;

        ChartRendererBase chartRenderer; 

        public ChartRequest Request
        {
            get { return (ChartRequest)DataContext; }
        }

        public QuerySettings Settings; 

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
                ((ChartRequest)e.NewValue).ChartRequestChanged -= ReDrawChart;

            if (e.NewValue != null)
                ((ChartRequest)e.NewValue).ChartRequestChanged += ReDrawChart;
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

            Settings = Navigator.Manager.GetQuerySettings(Request.QueryName);
            Description = Navigator.Manager.GetQueryDescription(Request.QueryName);
            var entityColumn = Description.StaticColumns.SingleOrDefault(a => a.IsEntity);
            EntityType = Reflector.ExtractLite(entityColumn.Type);

            qtbFilters.Token = null;
            qtbFilters.SubTokensEvent += new Func<QueryToken, QueryToken[]>(qtbFilters_SubTokensEvent);

            SetTitle(); 
        }

        void SetTitle()
        {
            tbEntityType.Text = EntityType.NicePluralName();

            string niceQueryName = QueryUtils.GetNiceQueryName(Request.QueryName);

            if (niceQueryName.StartsWith(tbEntityType.Text))
                niceQueryName = niceQueryName.Substring(tbEntityType.Text.Length).Trim();
            else
                niceQueryName = "- " + niceQueryName;

            tbQueryName.Text = niceQueryName;
        }

        QueryToken[] qtbFilters_SubTokensEvent(QueryToken arg)
        {
            if (arg == null)
                return (from s in Description.StaticColumns
                        where s.Filterable
                        select QueryToken.NewColumn(s)).ToArray();
            else
                return arg.SubTokens();
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
                    request.NeedNewQuery = false;
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
            if (!Request.NeedNewQuery)
            {
                if (resultTable != null)
                    SetResults();
            }
        }

        private bool HasErrors()
        {
            string errors = Request.IdentifiableIntegrityCheck();

            if (string.IsNullOrEmpty(errors))
                return false;

            MessageBox.Show(this.FindCurrentWindow(), "There are errors in the chart settings:\r\n" + errors, "Errors in the chart", MessageBoxButton.OK, MessageBoxImage.Stop);

            return true;
        }

        private void SetResults()
        {
            gvResults.Columns.Clear();
            foreach (var column in resultTable.Columns)
            {
                AddListViewColumn(column);
            }

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
                ChartRequest cr = (ChartRequest)DataContext;

                ChartTokenDN ct = cr.GetToken(ex.ChartTokenName);

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

        public void ClearResults()
        {
            resultTable = null;
            lvResult.ItemsSource = null;
            lvResult.Background = Brushes.WhiteSmoke;
        }

        GridViewColumn AddListViewColumn(Column c)
        {
            GridViewColumn column = new GridViewColumn
            {
                Header = new GridViewColumnHeader
                {
                    Content = c.DisplayName,
                    Tag = c,
                },
                CellTemplate = CreateDataTemplate(c),
            };
            gvResults.Columns.Add(column);
            return column;
        }

        DataTemplate CreateDataTemplate(Column c)
        {
            Binding b = new Binding("[{0}]".Formato(c.Index)) { Mode = BindingMode.OneTime };
            DataTemplate dt = Settings.GetFormatter(c)(b);
            return dt;
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
