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
using System.Threading;
using System.Windows.Threading;
using System.Windows.Documents;
using Signum.Windows.DynamicQuery;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Signum.Windows
{
    public partial class SearchControl
    {
        public static readonly DependencyProperty QueryNameProperty =
            DependencyProperty.Register("QueryName", typeof(object), typeof(SearchControl), new UIPropertyMetadata(null));
        public object QueryName
        {
            get { return (object)GetValue(QueryNameProperty); }
            set { SetValue(QueryNameProperty, value); }
        }

        public static readonly DependencyProperty OrderOptionsProperty =
          DependencyProperty.Register("OrderOptions", typeof(ObservableCollection<OrderOption>), typeof(SearchControl), new UIPropertyMetadata(null));
        public ObservableCollection<OrderOption> OrderOptions
        {
            get { return (ObservableCollection<OrderOption>)GetValue(OrderOptionsProperty); }
            set { SetValue(OrderOptionsProperty, value); }
        }

        public static readonly DependencyProperty FilterOptionsProperty =
           DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOption>), typeof(SearchControl), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> FilterOptions
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
        }

        public static readonly DependencyProperty ColumnOptionsModeProperty =
            DependencyProperty.Register("ColumnOptionsMode", typeof(ColumnOptionsMode), typeof(SearchControl), new UIPropertyMetadata(ColumnOptionsMode.Add));
        public ColumnOptionsMode ColumnOptionsMode
        {
            get { return (ColumnOptionsMode)GetValue(ColumnOptionsModeProperty); }
            set { SetValue(ColumnOptionsModeProperty, value); }
        }

        public static readonly DependencyProperty ColumnsOptionsProperty =
            DependencyProperty.Register("ColumnOptions", typeof(ObservableCollection<ColumnOption>), typeof(SearchControl), new UIPropertyMetadata(null));
        public ObservableCollection<ColumnOption> ColumnOptions
        {
            get { return (ObservableCollection<ColumnOption>)GetValue(ColumnsOptionsProperty); }
            set { SetValue(ColumnsOptionsProperty, value); }
        }

        public static readonly DependencyProperty AllowUserColumnsProperty =
            DependencyProperty.Register("AllowUserColumns", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool AllowUserColumns
        {
            get { return (bool)GetValue(AllowUserColumnsProperty); }
            set { SetValue(AllowUserColumnsProperty, value); }
        }

        public static readonly DependencyProperty ElementsPerPageProperty =
            DependencyProperty.Register("ElementsPerPage", typeof(int?), typeof(SearchControl), new UIPropertyMetadata(200));
        public int? ElementsPerPage
        {
            get { return (int?)GetValue(ElementsPerPageProperty); }
            set { SetValue(ElementsPerPageProperty, value); }
        }

        public static readonly DependencyProperty CurrentPageProperty =
           DependencyProperty.Register("CurrentPage", typeof(int), typeof(SearchControl), new UIPropertyMetadata(0));
        public int CurrentPage
        {
            get { return (int)GetValue(CurrentPageProperty); }
            set { SetValue(CurrentPageProperty, value); }
        }

        public static readonly DependencyProperty ItemsCountProperty =
            DependencyProperty.Register("ItemsCount", typeof(int), typeof(SearchControl), new UIPropertyMetadata(0));
        public int ItemsCount
        {
            get { return (int)GetValue(ItemsCountProperty); }
            set { SetValue(ItemsCountProperty, value); }
        }

        public static readonly DependencyProperty ShowFiltersProperty =
            DependencyProperty.Register("ShowFilters", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool ShowFilters
        {
            get { return (bool)GetValue(ShowFiltersProperty); }
            set { SetValue(ShowFiltersProperty, value); }
        }

        public static readonly DependencyProperty ShowFilterButtonProperty =
            DependencyProperty.Register("ShowFilterButton", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool ShowFilterButton
        {
            get { return (bool)GetValue(ShowFilterButtonProperty); }
            set { SetValue(ShowFilterButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowHeaderProperty =
            DependencyProperty.Register("ShowHeader", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool ShowHeader
        {
            get { return (bool)GetValue(ShowHeaderProperty); }
            set { SetValue(ShowHeaderProperty, value); }
        }

        public static readonly DependencyProperty ShowFooterProperty =
            DependencyProperty.Register("ShowFooter", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool ShowFooter
        {
            get { return (bool)GetValue(ShowFooterProperty); }
            set { SetValue(ShowFooterProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
          DependencyProperty.Register("SelectedItem", typeof(Lite), typeof(SearchControl), new UIPropertyMetadata(null));
        public Lite SelectedItem
        {
            get { return (Lite)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
          DependencyProperty.Register("SelectedItems", typeof(Lite[]), typeof(SearchControl), new UIPropertyMetadata(null));
        public Lite[] SelectedItems
        {
            get { return (Lite[])GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty MultiSelectionProperty =
            DependencyProperty.Register("MultiSelection", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool MultiSelection
        {
            get { return (bool)GetValue(MultiSelectionProperty); }
            set { SetValue(MultiSelectionProperty, value); }
        }

        public static readonly DependencyProperty SearchOnLoadProperty =
          DependencyProperty.Register("SearchOnLoad", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool SearchOnLoad
        {
            get { return (bool)GetValue(SearchOnLoadProperty); }
            set { SetValue(SearchOnLoadProperty, value); }
        }

        public static readonly DependencyProperty IsAdminProperty =
            DependencyProperty.Register("IsAdmin", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool IsAdmin
        {
            get { return (bool)GetValue(IsAdminProperty); }
            set { SetValue(IsAdminProperty, value); }
        }

        public static readonly DependencyProperty ViewProperty =
           DependencyProperty.Register("View", typeof(bool), typeof(SearchControl), new FrameworkPropertyMetadata(true, (d, e) => ((SearchControl)d).UpdateVisibility()));
        public bool View
        {
            get { return (bool)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        public static readonly DependencyProperty CreateProperty =
            DependencyProperty.Register("Create", typeof(bool), typeof(SearchControl), new FrameworkPropertyMetadata(true, (d, e) => ((SearchControl)d).UpdateVisibility()));
        public bool Create
        {
            get { return (bool)GetValue(CreateProperty); }
            set { SetValue(CreateProperty, value); }
        }

        public static readonly DependencyProperty ViewOnCreateProperty =
          DependencyProperty.Register("ViewOnCreate", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool ViewOnCreate
        {
            get { return (bool)GetValue(ViewOnCreateProperty); }
            set { SetValue(ViewOnCreateProperty, value); }
        }

        public Type EntityType
        {
            get { return Reflector.ExtractLite(entityColumn.Type); }
        }

        public Implementations Implementations
        {
            get { return entityColumn.Implementations; }
        }

        public static readonly DependencyProperty CollapseOnNoResultsProperty =
            DependencyProperty.Register("CollapseOnNoResults", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool CollapseOnNoResults
        {
            get { return (bool)GetValue(CollapseOnNoResultsProperty); }
            set { SetValue(CollapseOnNoResultsProperty, value); }
        }

        private void UpdateVisibility()
        {
            btCreate.Visibility = Create && EntityType != null ? Visibility.Visible : Visibility.Collapsed;
            UpdateViewSelection();
        }

        public event Func<IdentifiableEntity> Creating;
        public event Action<IdentifiableEntity> Viewing;
        public event Action DoubleClick;

        public SearchControl()
        {
            //ColumnDragController = new DragController(col => CreateFilter((GridViewColumnHeader)col), DragDropEffects.Copy);

            this.InitializeComponent();

            FilterOptions = new FreezableCollection<FilterOption>();
            OrderOptions = new ObservableCollection<OrderOption>();
            ColumnOptions = new ObservableCollection<ColumnOption>();
          
            this.Loaded += new RoutedEventHandler(SearchControl_Loaded);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(timer_Tick);
        }

        ColumnDescription entityColumn;

        IEnumerable<ColumnInfo> ColumnInfos
        {
            get { return gvResults.Columns.Select(gvc => (ColumnInfo)((GridViewColumnHeader)gvc.Header).Tag); }
        }

        public IEnumerable<Column> CurrentColumns
        {
            get { return ColumnInfos.Select(a => a.Column);  }
        }  

        ResultTable resultTable;
        public ResultTable ResultTable { get { return resultTable; } }
        public QuerySettings Settings { get; private set; }
        public QueryDescription Description { get; private set; }

        public static readonly RoutedEvent QueryResultChangedEvent = EventManager.RegisterRoutedEvent(
            "QueryResultChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchControl));
        public event RoutedEventHandler QueryResultChanged
        {
            add { AddHandler(QueryResultChangedEvent, value); }
            remove { RemoveHandler(QueryResultChangedEvent, value); }
        }

        void SearchControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SearchControl_Loaded;

            if (DesignerProperties.GetIsInDesignMode(this) || QueryName == null)
                return;

            Settings = Navigator.GetQuerySettings(QueryName);

            Description = Navigator.Manager.GetQueryDescription(QueryName);

            tokenBuilder.Token = null;
            tokenBuilder.SubTokensEvent += tokenBuilder_SubTokensEvent;

            entityColumn = Description.Columns.SingleOrDefault(a => a.IsEntity);
            if (entityColumn == null)
                throw new InvalidOperationException("Entity Column not found"); 

            if (this.NotSet(ViewProperty) && View && entityColumn.Implementations == null)
                View = Navigator.IsViewable(EntityType, IsAdmin);

            if (this.NotSet(CreateProperty) && Create && entityColumn.Implementations == null)
                Create = Navigator.IsCreable(EntityType, IsAdmin);

            GenerateListViewColumns();

            Navigator.Manager.SetFilterTokens(QueryName, FilterOptions);

            foreach (var fo in FilterOptions)
            {
                fo.ValueChanged += new EventHandler(fo_ValueChanged);
            }

            filterBuilder.Filters = FilterOptions;
            ((INotifyCollectionChanged)FilterOptions).CollectionChanged += FilterOptions_CollectionChanged;

            Navigator.Manager.SetOrderTokens(QueryName, OrderOptions);

            CompleteOrderColumns();

            if (GetCustomMenuItems != null)
            {
                MenuItem[] menus = GetCustomMenuItems.GetInvocationList().Cast<MenuItemForQueryName>().Select(d => d(QueryName, EntityType)).NotNull().ToArray();
                menu.Items.Clear();
                foreach (MenuItem mi in menus)
                {
                    menu.Items.Add(mi);
                }
            }

            if (SearchOnLoad)
            {
                if (IsVisible)
                    Search();
                else
                    IsVisibleChanged += SearchControl_IsVisibleChanged;
            }
        }

        void FilterOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMultiplyMessage();                       
        }

        QueryToken[] tokenBuilder_SubTokensEvent(QueryToken arg)
        {
            string canColumn = QueryUtils.CanColumn(arg);
            btCreateColumn.IsEnabled = string.IsNullOrEmpty(canColumn);
            btCreateColumn.ToolTip = canColumn;
         

            string canFilter = QueryUtils.CanFilter(arg);
            btCreateFilter.IsEnabled = string.IsNullOrEmpty(canFilter);
            btCreateFilter.ToolTip = canFilter;

            return QueryUtils.SubTokens(arg, Description.Columns);
        }

        private void CompleteOrderColumns()
        {
            for (int i = 0; i < OrderOptions.Count; i++)
            {
                OrderOption item = OrderOptions[i];
                QueryToken token = (QueryToken)item.Token;

                GridViewColumnHeader header = gvResults.Columns
                    .Select(c => (GridViewColumnHeader)c.Header)
                    .FirstOrDefault(c => ((ColumnInfo)c.Tag).Column.Name  == token.FullKey());
                if (header != null)
                    item.ColumnOrderInfo = new ColumnOrderInfo(header, item.OrderType, i);
            }
        }

        private void btCreateFilter_Click(object sender, RoutedEventArgs e)
        {
            filterBuilder.AddFilter(tokenBuilder.Token);
        }

        DispatcherTimer timer;
        void fo_ValueChanged(object sender, EventArgs e)
        {
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (resultTable != null)
            {
                Search();
            }

            timer.Stop();
        }

        void SearchControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (((bool)e.NewValue) == true)
            {
                IsVisibleChanged -= SearchControl_IsVisibleChanged;
                Search();
            }
        }

        void UpdateViewSelection()
        {
            btView.Visibility = lvResult.SelectedItem != null && View ? Visibility.Visible : Visibility.Collapsed;

            SelectedItem = ((ResultRow)lvResult.SelectedItem).TryCC(r => r.Entity);
            if (MultiSelection)
                SelectedItems = lvResult.SelectedItems.Cast<ResultRow>().Select(r => r.Entity).ToArray();
            else
                SelectedItems = null;
        }

        void GenerateListViewColumns()
        {   
            List<ColumnInfo> columnInfo = MergeColumnInfo();

            gvResults.Columns.Clear();

            foreach (var ci in columnInfo)
            {
                AddListViewColumn(ci);
            }
        }

        private List<ColumnInfo> MergeColumnInfo()
        {
            switch (ColumnOptionsMode)
            {
                case ColumnOptionsMode.Add:
                    return Description.Columns.Where(cd=>!cd.IsEntity).Select(cd => new ColumnInfo(new Column(cd))).Concat(ColumnOptions.Select(co => new ColumnInfo(co, Description))).ToList();
                case ColumnOptionsMode.Remove:
                    return Description.Columns.Where(cd => !cd.IsEntity && !ColumnOptions.Any(co => co.Path == cd.Name)).Select(cd => new ColumnInfo(new Column(cd))).ToList();
                case ColumnOptionsMode.Replace:
                    return ColumnOptions.Select(co => new ColumnInfo(co, Description)).ToList();
                default:
                    throw new InvalidOperationException("{0} is not a valid ColumnOptionMode".Formato(ColumnOptionsMode));
            }
        }

        void AddListViewColumn(ColumnInfo ci)
        {
            ci.GridViewColumn = new GridViewColumn
            {
                Header = new GridViewColumnHeader
                {
                    Content = ci.Column.DisplayName,
                    Tag = ci,
                    ContextMenu = (ContextMenu)FindResource("contextMenu")
                },
            };
            gvResults.Columns.Add(ci.GridViewColumn);
        }

        DataTemplate CreateDataTemplate(ResultColumn c)
        {
            Binding b = new Binding("[{0}]".Formato(c.Index)) { Mode = BindingMode.OneTime };
            DataTemplate dt = Settings.GetFormatter(c.Column)(b);
            return dt;
        }

        void FilterBuilder_SearchClicked(object sender, RoutedEventArgs e)
        {
            Search();
        }

        public void Search()
        {
            ClearResults();

            btFind.IsEnabled = false;

            var request = UpdateMultiplyMessage();

            Async.Do(this.FindCurrentWindow(),
                () => resultTable = Server.Return((IDynamicQueryServer s) => s.ExecuteQuery(request)),
                () =>
                {
                    if (resultTable != null)
                    {
                        SetResults();
                    }
                },
                () => { btFind.IsEnabled = true; });
        }

        public QueryRequest UpdateMultiplyMessage()
        {
            var result = GetQueryRequest();

            string message = CollectionElementToken.MultipliedMessage(result.Multiplications, EntityType);

            tbMultiplications.Text = message;
            brMultiplications.Visibility = message.HasText() ? Visibility.Visible : Visibility.Collapsed;

            return result;
        }

        public QueryRequest GetQueryRequest()
        {
            var request = new QueryRequest
            {
                QueryName = QueryName,
                Filters = FilterOptions.Select(f => f.ToFilter()).ToList(),
                Orders = OrderOptions.Select(o => o.ToOrder()).ToList(),
                Columns = CurrentColumns.ToList(),
                ElementsPerPage = ElementsPerPage
            };

            return request;
        }

        private void SetResults()
        {
            ColumnInfos.ZipForeach(resultTable.Columns, (ci, rc) =>
            {
                Debug.Assert(rc.Column.Token.Equals(ci.Column.Token));

                if (ci.ResultColumn == null || ci.ResultColumn.Index != rc.Index)
                    ci.GridViewColumn.CellTemplate = CreateDataTemplate(rc);

                ci.ResultColumn = rc; 
            });             

            lvResult.ItemsSource = resultTable.Rows;

            if (resultTable.Rows.Length > 0)
            {
                lvResult.SelectedIndex = 0;
                lvResult.ScrollIntoView(resultTable.Rows.First());
            }            
            ItemsCount = lvResult.Items.Count;
            lvResult.Background = Brushes.White;
            lvResult.Focus();
            elementsInPageLabel.Visibility = Visibility.Visible;
            elementsInPageLabel.TotalPages = resultTable.TotalPages;
            elementsInPageLabel.StartElementIndex = resultTable.StartElementIndex;
            elementsInPageLabel.EndElementIndex = resultTable.EndElementIndex;

            elementsPerPageSelector.ElementsPerPage = resultTable.ElementsPerPage;

            pageSelector.CurrentPage = resultTable.CurrentPage;
            pageSelector.TotalPages = resultTable.TotalPages;
            
            //tbResultados.Visibility = Visibility.Visible;
            //tbResultados.Foreground = resultTable.Rows.Length == ElementsPerPage ? Brushes.Red : Brushes.Black;
            OnQueryResultChanged(false);
        }

        public void ClearResults()
        {
            OnQueryResultChanged(true);
            resultTable = null;
            pageSelector.Visibility = Visibility.Hidden;
            elementsInPageLabel.Visibility = Visibility.Hidden;
            lvResult.ItemsSource = null;
            lvResult.Background = Brushes.WhiteSmoke;
        }

        void OnQueryResultChanged(bool cleaning)
        {
            if (!cleaning && CollapseOnNoResults)
                Visibility = resultTable.Rows.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

            RaiseEvent(new RoutedEventArgs(QueryResultChangedEvent));
        }

        void btView_Click(object sender, RoutedEventArgs e)
        {
            OnViewClicked();
        }

        void OnViewClicked()
        {
            ResultRow row = (ResultRow)lvResult.SelectedItem;

            if (row == null)
                return;

            IdentifiableEntity entity = (IdentifiableEntity)Server.Convert(row.Entity, EntityType);

            OnViewing(entity);
        }

        void btCreate_Click(object sender, RoutedEventArgs e)
        {
            OnCreate();
        }

        public Type SelectType()
        {
            if (Implementations == null)
                return EntityType;
            else if (Implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for this operation, override the event");
            else
                return Navigator.SelectType(this.FindCurrentWindow(), ((ImplementedByAttribute)Implementations).ImplementedTypes);
        }


        protected void OnCreate()
        {
            if (!Create)
                return;

            IdentifiableEntity result = Creating == null ? (IdentifiableEntity)Constructor.Construct(SelectType(), this.FindCurrentWindow()) : Creating();

            if (result == null)
                return;

            if (ViewOnCreate)
            {
                OnViewing(result);
            }
        }

        protected void OnViewing(IdentifiableEntity entity)
        {
            if (!View)
                return;

            if (this.Viewing == null)
                Navigator.NavigateUntyped(entity, new NavigateOptions { Admin = IsAdmin });
            else
                this.Viewing(entity);
        }

        void lvResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateViewSelection();
        }

        void lvResult_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DoubleClick != null)
                DoubleClick();
            else
                OnViewClicked();
            e.Handled = true;
        }

        void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader header = (GridViewColumnHeader)sender;
            ColumnInfo ci = (ColumnInfo)header.Tag;

            if (ci == null)
                return;

            string canOrder = QueryUtils.CanOrder(ci.Column.Token);
            if (canOrder.HasText())
            {
                MessageBox.Show(canOrder);
                return; 
            }

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift || (OrderOptions.Count == 1 && OrderOptions[0].ColumnOrderInfo.TryCC(coi=>coi.Header) == header))
            {

            }
            else
            {
                foreach (var oo in OrderOptions)
                {
                    if (oo.ColumnOrderInfo != null)
                        oo.ColumnOrderInfo.Clean();
                }

                OrderOptions.Clear();
            }

            OrderOption order = OrderOptions.SingleOrDefault(oo => oo.ColumnOrderInfo != null && oo.ColumnOrderInfo.Header == header);
            if (order != null)
            {
                order.ColumnOrderInfo.Flip();
                order.OrderType = order.ColumnOrderInfo.OrderType;
            }
            else
            {
                OrderOptions.Add(new OrderOption()
                {
                    Token = ci.Column.Token,
                    OrderType = OrderType.Ascending,
                    ColumnOrderInfo = new ColumnOrderInfo(header, OrderType.Ascending, OrderOptions.Count)
                });
            }

            Search();
        }

        public static event MenuItemForQueryName GetCustomMenuItems;

        FilterOption CreateFilter(GridViewColumnHeader header)
        {
            ColumnInfo column = (ColumnInfo)header.Tag;

            if (resultTable != null)
            {
                ResultRow row = (ResultRow)lvResult.SelectedItem;
                if (row != null)
                {
                    object value = row[column.ResultColumn];

                    return new FilterOption
                    {
                        Token = column.Column.Token,
                        Operation = FilterOperation.EqualTo,
                        Value = value is EmbeddedEntity ? null : value
                    };
                }
            }

            return new FilterOption
            {
                Token = column.Column.Token,
                Operation = FilterOperation.EqualTo,
                Value = FilterOption.DefaultValue(column.Column.Type),
            };
        }

        private void btCreateColumn_Click(object sender, RoutedEventArgs e)
        {
            QueryToken token = tokenBuilder.Token;

            AddColumn(token);

            UpdateMultiplyMessage(); 
        }

        private void AddColumn(QueryToken token)
        {
            if (!AllowUserColumns)
                return;

            string result = token.NiceName();
            if (ValueLineBox.Show<string>(ref result, Properties.Resources.NewColumnSName, Properties.Resources.ChooseTheDisplayNameOfTheNewColumn, Properties.Resources.Name, null, null, this.FindCurrentWindow()))
            {
                ClearResults();

                ColumnInfo ci = new ColumnInfo(new Column(token, result)); 

                AddListViewColumn(ci);
            }
        }

        private void lvResult_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(FilterOption)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void lvResult_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FilterOption)))
            {
                FilterOption filter = (FilterOption)e.Data.GetData(typeof(FilterOption));

                QueryToken token = filter.Token;

                AddColumn(filter.Token);
            }
        }

        private void renameMenu_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader gvch = (GridViewColumnHeader)((ContextMenu)(((MenuItem)sender).Parent)).PlacementTarget;

            ColumnInfo col = (ColumnInfo)gvch.Tag;
            string result = col.Column.DisplayName;
            if (ValueLineBox.Show<string>(ref result, Properties.Resources.NewColumnSName, Properties.Resources.ChooseTheDisplayNameOfTheNewColumn, Properties.Resources.Name, null, null, this.FindCurrentWindow()))
            {
                col.Column.DisplayName = result;
                gvch.Content = result;
            }
        }

        private void removeMenu_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader gvch = (GridViewColumnHeader)((ContextMenu)(((MenuItem)sender).Parent)).PlacementTarget;

            ColumnInfo co = (ColumnInfo)gvch.Tag;

            gvResults.Columns.Remove(co.GridViewColumn);

            UpdateMultiplyMessage(); 
        }

        private void filter_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader gvch = (GridViewColumnHeader)((ContextMenu)(((MenuItem)sender).Parent)).PlacementTarget;

            FilterOptions.Add(CreateFilter(gvch)); 
        }

        public void Reinitialize(List<FilterOption> filters, List<ColumnOption> columns, ColumnOptionsMode columnOptionsMode, List<OrderOption> orders)
        {
            ColumnOptions.Clear();
            ColumnOptions.AddRange(columns);
            ColumnOptionsMode = columnOptionsMode; 
            GenerateListViewColumns();

            FilterOptions.Clear();
            FilterOptions.AddRange(filters);
            Navigator.Manager.SetFilterTokens(QueryName, FilterOptions);

            OrderOptions.Clear();
            OrderOptions.AddRange(orders);
            Navigator.Manager.SetOrderTokens(QueryName, OrderOptions);
            CompleteOrderColumns();


            UpdateMultiplyMessage(); 
        }

        private void btFilters_Unchecked(object sender, RoutedEventArgs e)
        {
            rowFilters.Height = new GridLength(); //Auto
        }


    }

    public delegate SearchControlMenuItem MenuItemForQueryName(object queryName, Type entityType);

    public class SearchControlMenuItem : MenuItem
    {
        public SearchControl SearchControl { get; set; }

        public SearchControlMenuItem() { }
        public SearchControlMenuItem(RoutedEventHandler onClick)
        {
            this.Click += onClick;
        }

        protected override void OnInitialized(EventArgs e)
        {
            this.Loaded += new RoutedEventHandler(SearchControlMenuItem_Loaded);
            base.OnInitialized(e);
        }

        void SearchControlMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SearchControlMenuItem_Loaded;
            if (this.Parent != null)
            {
                SearchControl result = this.LogicalParents().OfType<SearchControl>().First();
             
                if (result is SearchControl)
                {
                    SearchControl = (SearchControl)result;

                    SearchControl.QueryResultChanged += new RoutedEventHandler(searchControl_QueryResultChanged);

                    Initialize();
                }
            }
        }

        void searchControl_QueryResultChanged(object sender, RoutedEventArgs e)
        {
            QueryResultChanged();
        }

        public virtual void Initialize()
        {
            foreach (var item in Items.OfType<SearchControlMenuItem>())
            {
                item.SearchControl = this.SearchControl;
                item.Initialize();
            }
        }

        public virtual void QueryResultChanged()
        {
            foreach (var item in Items.OfType<SearchControlMenuItem>())
            {
                item.QueryResultChanged();
            }
        }


    }

   
}
