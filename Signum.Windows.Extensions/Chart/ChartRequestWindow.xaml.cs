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
        public Type EntityType;

        public ChartRequest Request
        {
            get { return (ChartRequest)DataContext; }
            set
            {
                DataContext = value;
                chartRenderer.UpdateFiltersOrdersUserInterface();
            }
        }

        public ChartRequestWindow()
        {
            InitializeComponent();

            //rendererPlaceHolder.Children.Insert(0, chartRenderer); 

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ChartBuilder_DataContextChanged);
            this.Loaded += new RoutedEventHandler(ChartWindow_Loaded);
            chartRenderer.GenerateChart += GenerateChart;

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
            filterBuilder.Filters = chartRenderer.FilterOptions = new FreezableCollection<FilterOption>();

            chartRenderer.UpdateFiltersOrdersUserInterface();

            ((INotifyCollectionChanged)filterBuilder.Filters).CollectionChanged += Filters_CollectionChanged;
            Request.ChartRequestChanged += Request_ChartRequestChanged;

            chartBuilder.Description = DynamicQueryServer.GetQueryDescription(Request.QueryName);

            var entityColumn = chartBuilder.Description.Columns.SingleOrDefaultEx(a => a.IsEntity);
            EntityType = Lite.Extract(entityColumn.Type);

            qtbFilters.Token = null;
            qtbFilters.SubTokensEvent += new Func<QueryToken, List<QueryToken>>(qtbFilters_SubTokensEvent);

            SetTitle();
        }

        void Request_ChartRequestChanged()
        {
            UpdateMultiplyMessage();
            chartRenderer.ReDrawChart(); 
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
            if (cr == null || chartRenderer.Description == null)
                return new List<QueryToken>();

            return token.SubTokens(chartRenderer.Description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | (cr.GroupResults ? SubTokensOptions.CanAggregate : 0));
        }

        private void btCreateFilter_Click(object sender, RoutedEventArgs e)
        {
            filterBuilder.AddFilter(qtbFilters.Token);
        }

      

        private void execute_Click(object sender, RoutedEventArgs e)
        {
            GenerateChart();
        }


        public void GenerateChart()
        {
            Request.Filters = chartRenderer.FilterOptions.Select(f => f.ToFilter()).ToList();
            Request.Orders = chartRenderer.OrderOptions.Select(o => o.ToOrder()).ToList();

            var request = Request;

            if (HasErrors())
                return;

            execute.IsEnabled = false;
            Async.Do(
                () => chartRenderer.ResultTable = Server.Return((IChartServer cs) => cs.ExecuteChart(request)),
                () =>
                {
                    request.NeedNewQuery = false;
                    chartRenderer.ReDrawChart();
                },
                () => execute.IsEnabled = true);
        }

        private bool HasErrors()
        {
            GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(Request));

            var errors = Request.FullIntegrityCheck();

            if (errors == null)
                return false;

            MessageBox.Show(Window.GetWindow(this), "There are errors in the chart settings:\r\n" + errors, "Errors in the chart", MessageBoxButton.OK, MessageBoxImage.Stop);

            return true;
        }



        private void edit_Click(object sender, RoutedEventArgs e)
        {
            Navigator.Navigate(Request.ChartScript.ToLite().Retrieve(), new NavigateOptions()
            {
                View = () => new ChartScript { RequestWindow = this },
                Clone = false,
            });
        }

        internal void SetResults(string script)
        {
            chartRenderer.SetResults(script);
        }
    }
}
