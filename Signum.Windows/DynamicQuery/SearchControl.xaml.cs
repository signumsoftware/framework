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
using System.Windows.Automation.Peers;
using System.Windows.Automation;

namespace Signum.Windows
{
    public partial class SearchControl
    {
        public static readonly DependencyProperty QueryNameProperty =
            DependencyProperty.Register("QueryName", typeof(object), typeof(SearchControl), new UIPropertyMetadata((o,s)=>((SearchControl)o).QueryNameChanged(s)));
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


        public static readonly DependencyProperty SimpleFilterBuilderProperty =
          DependencyProperty.Register("SimpleFilterBuilder", typeof(ISimpleFilterBuilder), typeof(SearchControl), new UIPropertyMetadata(null, (d, e) => ((SearchControl)d).SimpleFilterBuilderChanged(e)));
        public ISimpleFilterBuilder SimpleFilterBuilder
        {
            get { return (ISimpleFilterBuilder)GetValue(SimpleFilterBuilderProperty); }
            set { SetValue(SimpleFilterBuilderProperty, value); }
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

        public static readonly DependencyProperty AllowChangeColumnsProperty =
            DependencyProperty.Register("AllowChangeColumns", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool AllowChangeColumns
        {
            get { return (bool)GetValue(AllowChangeColumnsProperty); }
            set { SetValue(AllowChangeColumnsProperty, value); }
        }

        public static readonly DependencyProperty PaginationProperty =
            DependencyProperty.Register("Pagination", typeof(Pagination), typeof(SearchControl), new PropertyMetadata(new Pagination.All(),
                (s, e) => ((SearchControl)s).Pagination_Changed((Pagination)e.OldValue, (Pagination)e.NewValue)));
        public Pagination Pagination
        {
            get { return (Pagination)GetValue(PaginationProperty); }
            set { SetValue(PaginationProperty, value); }
        }

        public static readonly DependencyProperty ItemsCountProperty =
            DependencyProperty.Register("ItemsCount", typeof(int), typeof(SearchControl), new UIPropertyMetadata(0));
        public int ItemsCount
        {
            get { return (int)GetValue(ItemsCountProperty); }
            set { SetValue(ItemsCountProperty, value); }
        }

        public static readonly DependencyProperty ShowFiltersProperty =
            DependencyProperty.Register("ShowFilters", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false, (s, e) => ((SearchControl)s).ShowFiltersChanged(e)));
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

        public static readonly DependencyProperty ShowFindButtonProperty =
            DependencyProperty.Register("ShowFindButton", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool ShowFindButton
        {
            get { return (bool)GetValue(ShowFindButtonProperty); }
            set { SetValue(ShowFindButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowHeaderProperty =
            DependencyProperty.Register("ShowHeader", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool ShowHeader
        {
            get { return (bool)GetValue(ShowHeaderProperty); }
            set { SetValue(ShowHeaderProperty, value); }
        }

        public static readonly DependencyProperty ShowFooterProperty =
            DependencyProperty.Register("ShowFooter", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool ShowFooter
        {
            get { return (bool)GetValue(ShowFooterProperty); }
            set { SetValue(ShowFooterProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
          DependencyProperty.Register("SelectedItem", typeof(Lite<Entity>), typeof(SearchControl), new UIPropertyMetadata(null));
        public Lite<Entity> SelectedItem
        {
            get { return (Lite<Entity>)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
          DependencyProperty.Register("SelectedItems", typeof(List<Lite<Entity>>), typeof(SearchControl), new UIPropertyMetadata(null));
        public List<Lite<Entity>> SelectedItems
        {
            get { return (List<Lite<Entity>>)GetValue(SelectedItemsProperty); }
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

        public static readonly DependencyProperty NavigateProperty =
           DependencyProperty.Register("Navigate", typeof(bool), typeof(SearchControl), new FrameworkPropertyMetadata(true, (d, e) => ((SearchControl)d).UpdateVisibility()));
        public bool Navigate
        {
            get { return (bool)GetValue(NavigateProperty); }
            set { SetValue(NavigateProperty, value); }
        }

        public static readonly DependencyProperty CreateProperty =
            DependencyProperty.Register("Create", typeof(bool), typeof(SearchControl), new FrameworkPropertyMetadata(true, (d, e) => ((SearchControl)d).UpdateVisibility()));
        public bool Create
        {
            get { return (bool)GetValue(CreateProperty); }
            set { SetValue(CreateProperty, value); }
        }

        public static readonly DependencyProperty RemoveProperty =
            DependencyProperty.Register("Remove", typeof(bool), typeof(SearchControl), new FrameworkPropertyMetadata(false, (d, e) => ((SearchControl)d).UpdateVisibility()));
        public bool Remove
        {
            get { return (bool)GetValue(RemoveProperty); }
            set { SetValue(RemoveProperty, value); }
        }

        public static readonly DependencyProperty NavigateOnCreateProperty =
          DependencyProperty.Register("NavigateOnCreate", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool NavigateOnCreate
        {
            get { return (bool)GetValue(NavigateOnCreateProperty); }
            set { SetValue(NavigateOnCreateProperty, value); }
        }

        public static readonly DependencyProperty IsSearchingProperty =
          DependencyProperty.Register("IsSearching", typeof(bool), typeof(SearchControl), new PropertyMetadata(false));
        public bool IsSearching
        {
            get { return (bool)GetValue(IsSearchingProperty); }
            set { SetValue(IsSearchingProperty, value); }
        }

        public static readonly DependencyProperty FilterColumnProperty =
        DependencyProperty.Register("FilterColumn", typeof(string), typeof(SearchControl), new UIPropertyMetadata(null, 
            (d, e) => ((SearchControl)d).AssetNotLoaded(e)));
        public string FilterColumn
        {
            get { return (string)GetValue(FilterColumnProperty); }
            set { SetValue(FilterColumnProperty, value); }
        }

        public static readonly DependencyProperty FilterRouteProperty =
            DependencyProperty.Register("FilterRoute", typeof(string), typeof(SearchControl), new UIPropertyMetadata(null, 
                (d, e) => ((SearchControl)d).AssetNotLoaded(e)));
        public string FilterRoute
        {
            get { return (string)GetValue(FilterRouteProperty); }
            set { SetValue(FilterRouteProperty, value); }
        }

        public bool CanAddFilters
        {
            get { return this.ShowHeader && (this.ShowFilters || this.ShowFilterButton); }
        }

        private void AssetNotLoaded(DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded)
                throw new InvalidProgramException("You can not change {0} property once loaded".FormatWith(e.Property));
        }

        private void SimpleFilterBuilderChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                ShowFilters = false;
            }
        }

        private void ShowFiltersChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true && SimpleFilterBuilder != null)
            {
                RefreshSimpleFilters();

                SimpleFilterBuilder = null;
            }
        }


        public Type EntityType
        {
            get { return entityColumn == null ? null : Lite.Extract(entityColumn.Type); }
        }

        public Implementations Implementations
        {
            get { return entityColumn.Implementations.Value; }
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
            btSearch.Visibility = ShowFindButton ? Visibility.Visible : Visibility.Collapsed;

            UpdateViewSelection();
        }

        public event Func<Entity> Creating;
        public event Action<Entity> Navigating;
        public event Action<List<Lite<Entity>>> Removing;
        public event Action DoubleClick;
        public event Func<Column, bool> OrderClick;

        public SearchControl()
        {
            this.InitializeComponent();

            FilterOptions = new FreezableCollection<FilterOption>();
            OrderOptions = new ObservableCollection<OrderOption>();
            ColumnOptions = new ObservableCollection<ColumnOption>();
            this.Loaded += new RoutedEventHandler(SearchControl_Loaded);
        }

        private void QueryNameChanged(DependencyPropertyChangedEventArgs s)
        {
            if (DesignerProperties.GetIsInDesignMode(this) || s.NewValue == null)
            {
                return;
            }

            if (!Finder.IsFindable(s.NewValue))
            {
                Common.VoteCollapsed(this);
                return;
            }

            Common.VoteVisible(this);


            Settings = Finder.GetQuerySettings(s.NewValue);

            Description = DynamicQueryServer.GetQueryDescription(s.NewValue);

            if (Settings.SimpleFilterBuilder != null)
            {
                SimpleFilterBuilder = Settings.SimpleFilterBuilder(Description);
            }

            tokenBuilder.Token = null;
            tokenBuilder.SubTokensEvent += tokenBuilder_SubTokensEvent;

            entityColumn = Description.Columns.SingleOrDefaultEx(a => a.IsEntity);
            if (entityColumn == null)
                throw new InvalidOperationException("Entity Column not found on {0}".FormatWith(QueryUtils.GetKey(QueryName)));
        }

        ColumnDescription entityColumn;

        ResultTable resultTable;
        public ResultTable ResultTable { get { return resultTable; } }
        public QuerySettings Settings { get; private set; }
        public QueryDescription Description { get; private set; }

        public static readonly RoutedEvent ResultChangedEvent = EventManager.RegisterRoutedEvent(
            "ResultChangedEvent", RoutingStrategy.Bubble, typeof(ResultChangedEventHandler), typeof(SearchControl));
        public event ResultChangedEventHandler ResultChanged
        {
            add { AddHandler(ResultChangedEvent, value); }
            remove { RemoveHandler(ResultChangedEvent, value); }
        }


        void SearchControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SearchControl_Loaded;

            if (DesignerProperties.GetIsInDesignMode(this) || QueryName == null || !Finder.IsFindable(QueryName))
            {
                tokenBuilder.Token = null;
                tokenBuilder.SubTokensEvent += q => new List<QueryToken>();

                return;
            }

            Common.VoteVisible(this);

            OnLoaded();
        }

        bool loaded; 
        public void OnLoaded()
        {
            if (loaded)
                return;

            loaded = true;

            if (FilterColumn.HasText())
            {
                FilterOptions.Add(new FilterOption
                {
                    ColumnName = FilterColumn,
                    Operation = FilterOperation.EqualTo,
                    Frozen = true,
                }.Bind(FilterOption.ValueProperty, new Binding("DataContext" + (FilterRoute.HasText() ? "." + FilterRoute : null)) { Source = this }));

                if (QueryUtils.IsColumnToken(FilterColumn))
                {
                    ColumnOptions.Add(new ColumnOption(FilterColumn));
                    ColumnOptionsMode = ColumnOptionsMode.Remove;
                }
                if (this.NotSet(SearchOnLoadProperty))
                    SearchOnLoad = true;
            }

            if (OrderOptions.IsNullOrEmpty() && !entityColumn.Implementations.Value.IsByAll)
            {
                var orderType = entityColumn.Implementations.Value.Types.All(t => EntityKindCache.GetEntityData(t) == EntityData.Master) ? OrderType.Ascending : OrderType.Descending;

                var column = Description.Columns.SingleOrDefaultEx(c => c.Name == "Id");

                if (column != null)
                {
                    OrderOptions.Add(new OrderOption(column.Name, orderType));
                }
            }

            btCreate.ToolTip = SearchMessage.CreateNew0_G.NiceToString()
                .ForGenderAndNumber(entityColumn.Implementations.Value.Types.FirstOrDefault()?.GetGender() ?? 'm')
                .FormatWith(entityColumn.Implementations.Value.Types.CommaOr(a => a.NiceName()));

            if (this.NotSet(SearchControl.NavigateProperty) && Navigate)
                Navigate = Implementations.IsByAll ? true :
                           Implementations.Types.Any(t => Navigator.IsNavigable(t, isSearch: true));

            if (this.NotSet(EntityBase.CreateProperty) && Create)
                Create = Implementations.IsByAll ? false :
                         Implementations.Types.Any(t => Navigator.IsCreable(t, isSearch: true));

            ColumnOption.SetColumnTokens(ColumnOptions, Description);

            if (this.CanAddFilters || this.AllowChangeColumns)
            {
                headerContextMenu = new ContextMenu();

                if (this.CanAddFilters)
                    headerContextMenu.Items.Add(new MenuItem { Header = SearchMessage.AddFilter.NiceToString() }.Handle(MenuItem.ClickEvent, filterHeader_Click));

                if (this.CanAddFilters && this.AllowChangeColumns)
                    headerContextMenu.Items.Add(new Separator());

                if (this.AllowChangeColumns)
                {
                    headerContextMenu.Items.Add(new MenuItem { Header = SearchMessage.Rename.NiceToString() }.Handle(MenuItem.ClickEvent, renameMenu_Click));
                    headerContextMenu.Items.Add(new MenuItem { Header = EntityControlMessage.Remove.NiceToString() }.Handle(MenuItem.ClickEvent, removeMenu_Click));
                }
            }

            GenerateListViewColumns();

            FilterOption.SetFilterTokens(FilterOptions, Description);

            filterBuilder.Filters = FilterOptions;
            ((INotifyCollectionChanged)FilterOptions).CollectionChanged += FilterOptions_CollectionChanged;

            OrderOption.SetOrderTokens(OrderOptions, Description);

            SortGridViewColumnHeader.SetColumnAdorners(gvResults, OrderOptions);

            if (IsVisible)
            {
                FillMenuItems();

                if (SearchOnLoad)
                    Search();
            }
            else
                IsVisibleChanged += SearchControl_IsVisibleChanged;

            UpdateVisibility();

            AutomationProperties.SetName(this, QueryUtils.GetKey(QueryName));

            foreach (var item in FilterOptions)
            {
                item.BindingValueChanged += new DependencyPropertyChangedEventHandler(item_BindingValueChanged);
            }
        }

        ContextMenu headerContextMenu = null;
       
        void FilterOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMultiplyMessage(false);                       
        }

        List<QueryToken> tokenBuilder_SubTokensEvent(QueryToken arg)
        {
            string canColumn = QueryUtils.CanColumn(arg);
            btCreateColumn.IsEnabled = string.IsNullOrEmpty(canColumn);
            btCreateColumn.ToolTip = canColumn;
         

            string canFilter = QueryUtils.CanFilter(arg);
            btCreateFilter.IsEnabled = string.IsNullOrEmpty(canFilter);
            btCreateFilter.ToolTip = canFilter;

            return arg.SubTokens(Description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement);
        }

        private void btCreateFilter_Click(object sender, RoutedEventArgs e)
        {
            filterBuilder.AddFilter(tokenBuilder.Token);
        }

        void item_BindingValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (hasBeenLoaded && e.NewValue != null)
            {
                Search();
            }
        }

        void SearchControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (((bool)e.NewValue) == true)
            {
                IsVisibleChanged -= SearchControl_IsVisibleChanged;

                FillMenuItems();

                if (SearchOnLoad)
                    Search();
            }
        }

        public static event Func<SearchControl, MenuItem> GetMenuItems;
        public static event Func<SearchControl, IEnumerable<MenuItem>> GetContextMenuItems;

        private void FillMenuItems()
        {
            if (GetMenuItems != null)
            {
                List<MenuItem> items = GetMenuItems.GetInvocationListTyped().Select(d => d(this)).NotNull().ToList();
                menu.Items.Clear();
                foreach (MenuItem mi in items)
                    menu.Items.Add(mi);
            }
        }

        private void contextMenu_Opened(object sender, RoutedEventArgs e)
        {
            FillContextMenuItems();
        }

        private void FillContextMenuItems()
        {
            contextMenu.Items.Clear();

            if (this.CanAddFilters && GetCellColumnHeader(contextMenu) != null)
            {
                contextMenu.Items.Add(new MenuItem { Header = SearchMessage.AddFilter.NiceToString() }.Handle(MenuItem.ClickEvent, filterCell_Click));
            } 

            if (GetContextMenuItems != null)
            {
                foreach (var fun in GetContextMenuItems.GetInvocationListTyped())
                {
                    var items = fun(this)?.ToList();

                    if (items.IsNullOrEmpty())
                        continue;

                    if (contextMenu.Items.Count > 0)
                        contextMenu.Items.Add(new Separator());

                    foreach (var item in items)
                        contextMenu.Items.Add(item);
                }

                if (contextMenu.Items.Count == 0)
                    contextMenu.Items.Add(new MenuItem { Header = new TextBlock(new Italic(new Run(SearchMessage.NoActionsFound.NiceToString()))), IsEnabled = false });
            }

            ContextMenuOpened?.Invoke(contextMenu);
        }

        public event Action<ContextMenu> ContextMenuOpened;
        
        void UpdateViewSelection()
        {
            btView.Visibility = Navigate && lvResult.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
            btRemove.Visibility = Remove && lvResult.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;

            SelectedItem = ((ResultRow)lvResult.SelectedItem)?.Entity;
            if (MultiSelection)
                SelectedItems = lvResult.SelectedItems.Cast<ResultRow>().Select(r => r.Entity).ToList();
            else
                SelectedItems = null;
        }

        public void SetDirtySelectedItems()
        {
            foreach (var rr in lvResult.SelectedItems.Cast<ResultRow>())
            {
                rr.IsDirty = true;
            }
        }

        public void SetDirtySelectedItem()
        {
            var rr = (ResultRow)lvResult.SelectedItem;
            if (rr == null)
                return;

            rr.IsDirty = true;
        }

        public void GenerateListViewColumns()
        {
            if (IsSearching)
            {
                generateListViewColumnsQueued = true;
                return;
            }

            List<Column> columns = ColumnOption.MergeColumns(ColumnOptions, ColumnOptionsMode, Description);

            gvResults.Columns.Clear();

            foreach (var co in columns)
            {
                AddListViewColumn(co);
            }
        }

        void AddListViewColumn(Column col)
        {
            GridViewColumn gvc = new GridViewColumn
            {
                Header = new SortGridViewColumnHeader
                {
                    Content = col.DisplayName,
                    ContextMenu = headerContextMenu,
                    RequestColumn = col,
                },
            };
            gvResults.Columns.Add(gvc);
        }

        DataTemplate CreateDataTemplate(ResultColumn c)
        {
            Binding b = new Binding("[{0}]".FormatWith(c.Index)) { Mode = BindingMode.OneTime };
            DataTemplate dt = Settings.GetFormatter(c.Column)(b);
            return dt;
        }

        void btSearch_Click(object sender, RoutedEventArgs e)
        {
            Search(resetPage: true);
        }


        bool hasBeenLoaded = false;


        bool? searchQueuedResetPage;
        bool generateListViewColumnsQueued; 

        public void Search(bool resetPage = true)
        {
            if (IsSearching)
            {
                searchQueuedResetPage = resetPage;
                return;
            }

            ClearResults();

            IsSearching = true;

            var pag = Pagination as Pagination.Paginate;
            if (resetPage && pag != null && pag.CurrentPage != 1)
            {
                try
                {
                    avoidPaginationChange = true;
                    Pagination = new Pagination.Paginate(pag.ElementsPerPage, 1);
                }
                finally
                {
                    avoidPaginationChange = false;
                }
            }

            QueryRequest request = UpdateMultiplyMessage(true);

            request.QueryBatch(rt =>
            {
                hasBeenLoaded = true;

                resultTable = rt;

                if (rt != null)
                {
                    SetResults(rt);
                }
            },
            () =>
            {
                IsSearching = false;
                if (generateListViewColumnsQueued)
                {
                    generateListViewColumnsQueued = false;
                    GenerateListViewColumns();
                }
                if (searchQueuedResetPage != null)
                {
                    var c = searchQueuedResetPage.Value;
                    searchQueuedResetPage = null;
                    Search(c); 
                }
            });
        }

        public QueryRequest UpdateMultiplyMessage(bool updateSimpleFilters)
        {
            var result = GetQueryRequest(updateSimpleFilters);

            string message = CollectionElementToken.MultipliedMessage(result.Multiplications(), EntityType);

            tbMultiplications.Text = message;
            brMultiplications.Visibility = message.HasText() ? Visibility.Visible : Visibility.Collapsed;

            return result;
        }

        public QueryRequest GetQueryRequest(bool updateSimpleFilters)
        {
            if (updateSimpleFilters)
                RefreshSimpleFilters();

            var request = new QueryRequest
            {
                QueryName = QueryName,
                Filters = FilterOptions.Select(f => f.ToFilter()).ToList(),
                Orders = OrderOptions.Select(o => o.ToOrder()).ToList(),
                Columns = gvResults.Columns.Select(gvc => ((SortGridViewColumnHeader)gvc.Header).RequestColumn).ToList(),
                Pagination = Pagination,
            };

            return request;
        }

        private void RefreshSimpleFilters()
        {
            if (SimpleFilterBuilder != null)
            {
                FilterOptions.Clear();
                var newFilters = SimpleFilterBuilder.GenerateFilterOptions();

                FilterOption.SetFilterTokens(newFilters, Description);
                FilterOptions.AddRange(newFilters);
            }
        }


        bool avoidPaginationChange = false;
        private void SetResults(ResultTable rt)
        {
            try
            {
                avoidPaginationChange = true;

                gvResults.Columns.ZipForeach(rt.Columns, (gvc, rc) =>
                {
                    var header = (SortGridViewColumnHeader)gvc.Header;

                    if (!rc.Column.Token.Equals(header.RequestColumn.Token))
                        throw new InvalidOperationException("The token in the ResultColumn ({0}) does not match with the token in the GridView ({1})"
                            .FormatWith(rc.Column.Token.FullKey(), header.RequestColumn.Token.FullKey()));

                    if (header.ResultColumn == null || header.ResultColumn.Index != rc.Index)
                        gvc.CellTemplate = CreateDataTemplate(rc);

                    header.ResultColumn = rc;
                });

                lvResult.ItemsSource = rt.Rows;

                foreach (GridViewColumn column in gvResults.Columns)
                {
                    if (double.IsNaN(column.Width))
                        column.Width = column.ActualWidth;

                    column.Width = double.NaN;
                }

                if (rt.Rows.Length > 0)
                {
                    lvResult.SelectedIndex = 0;
                    lvResult.ScrollIntoView(rt.Rows.FirstEx());
                }
                ItemsCount = lvResult.Items.Count;
                lvResult.Background = Brushes.White;
                lvResult.Focus();
                paginationSelector.elementsInPageLabel.Visibility = Visibility.Visible;
                paginationSelector.elementsInPageLabel.SetResults(rt);
                paginationSelector.Visibility = System.Windows.Visibility.Visible;
                paginationSelector.TotalPages = rt.TotalPages;

                //tbResultados.Visibility = Visibility.Visible;
                //tbResultados.Foreground = rt.Rows.Length == ElementsPerPage ? Brushes.Red : Brushes.Black;
                OnQueryResultChanged(false);
            }
            finally
            {
                avoidPaginationChange = false;
            }
        }

        public void ClearResults()
        {
            OnQueryResultChanged(true);
            resultTable = null;
            paginationSelector.elementsInPageLabel.Visibility = Visibility.Hidden;
            paginationSelector.TotalPages = null;
            lvResult.ItemsSource = null;
            lvResult.Background = Brushes.WhiteSmoke;
        }

        public event EventHandler FixSize;
        public event EventHandler ClearSize;


        private void Pagination_Changed(Pagination oldValue, Pagination newValue)
        {
            if (!IsLoaded || avoidPaginationChange)
                return;

            var oldPaginate = oldValue as Pagination.Paginate;
            var newPaginate = newValue as Pagination.Paginate;

            if (oldPaginate != null && newPaginate != null &&
                oldPaginate.ElementsPerPage == newPaginate.ElementsPerPage &&
                oldPaginate.CurrentPage != newPaginate.CurrentPage)
            {
                FixSize?.Invoke(this, new EventArgs());
            }
            else
            {
                ClearSize?.Invoke(this, new EventArgs());
            }

            if (newValue is Pagination.All)
                ClearResults();
            else
                Search(resetPage: false);
        }

        void OnQueryResultChanged(bool cleaning)
        {
            if (!cleaning && CollapseOnNoResults)
                Visibility = resultTable.Rows.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

            RaiseEvent(new ResultChangedEventArgs(ResultChangedEvent, cleaning));
        }

        void btView_Click(object sender, RoutedEventArgs e)
        {
            OnNavigateClicked();
        }

        void OnNavigateClicked()
        {
            ResultRow row = (ResultRow)lvResult.SelectedItem;

            if (row == null)
                return;

            SetDirtySelectedItem();

            Entity entity = (Entity)Server.Convert(row.Entity, EntityType);

            OnNavigating(entity);
        }

        void btCreate_Click(object sender, RoutedEventArgs e)
        {
            OnCreate();
        }

        public Type SelectType(Func<Type, bool> filterType)
        {
            if (Implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for this operation, override the event");

            return Navigator.SelectType(Window.GetWindow(this), Implementations.Types, filterType);
        }


        protected void OnCreate()
        {
            if (!Create)
                return;

            Entity result = Creating != null ? Creating() :
                SelectType(t => Navigator.IsCreable(t, isSearch: true))
                ?.Let(type => (Entity)new ConstructorContext(this).ConstructUntyped(type));

            if (result == null)
                return;

            if (NavigateOnCreate)
            {
                OnNavigating(result);
            }
        }

        protected void OnNavigating(Entity entity)
        {
            if (!Navigate)
                return;

            if (this.Navigating == null)
                Navigator.NavigateUntyped(entity);
            else
                this.Navigating(entity);
        }

        void btRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lvResult.SelectedItems.Count == 0)
                return;

            var lites = lvResult.SelectedItems.Cast<ResultRow>().Select(r => r.Entity).ToList();

            if (this.Removing == null)
                throw new InvalidOperationException("Remove event not set");

            this.Removing(lites);
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
                OnNavigateClicked();
            e.Handled = true;
        }

        void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            SortGridViewColumnHeader header = sender as SortGridViewColumnHeader;

            if (header == null)
                return;

            if (OrderClick != null && !OrderClick(header.RequestColumn))
                return;

            string canOrder = QueryUtils.CanOrder(header.RequestColumn.Token);
            if (canOrder.HasText())
            {
                //Avoid UI Automation hangs
                Dispatcher.BeginInvoke(() => MessageBox.Show(Window.GetWindow(this), canOrder)); 

                return; 
            }

            header.ChangeOrders(OrderOptions);

            Search(resetPage: true);
        }

      

        private void btCreateColumn_Click(object sender, RoutedEventArgs e)
        {
            QueryToken token = tokenBuilder.Token;

            AddColumn(token);

            UpdateMultiplyMessage(true); 
        }

        private void AddColumn(QueryToken token)
        {
            if (!AllowChangeColumns)
                return;

            string result = token.NiceName();
            if (ValueLineBox.Show<string>(ref result, SearchMessage.NewColumnSName.NiceToString(), SearchMessage.ChooseTheDisplayNameOfTheNewColumn.NiceToString(), SearchMessage.Name.NiceToString(), null, null, Window.GetWindow(this)))
            {
                ClearResults();

                AddListViewColumn(new Column(token, result));
            }
        }

        private void renameMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!AllowChangeColumns)
                return;

            SortGridViewColumnHeader gvch = GetHeaderColumnHeader(sender);

            string result = gvch.RequestColumn.DisplayName;
            if (ValueLineBox.Show<string>(ref result, SearchMessage.NewColumnSName.NiceToString(), SearchMessage.ChooseTheDisplayNameOfTheNewColumn.NiceToString(), SearchMessage.Name.NiceToString(), null, null, Window.GetWindow(this)))
            {
                gvch.RequestColumn.DisplayName = result;
                gvch.Content = result;
            }
        }

        private static SortGridViewColumnHeader GetHeaderColumnHeader(object sender)
        {
            var context = (ContextMenu)((MenuItem)sender).Parent;

            return (SortGridViewColumnHeader)context.PlacementTarget;
        }

        private void removeMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!AllowChangeColumns)
                return;

            SortGridViewColumnHeader gvch = GetHeaderColumnHeader(sender);

            gvResults.Columns.Remove(gvch.Column);

            UpdateMultiplyMessage(true); 
        }

        private void filterHeader_Click(object sender, RoutedEventArgs e)
        {
            SortGridViewColumnHeader gvch = GetHeaderColumnHeader(sender);

            FilterOptions.Add(new FilterOption
            {
                Token = gvch.RequestColumn.Token,
                Operation = FilterOperation.EqualTo,
                Value = FilterOption.DefaultValue(gvch.RequestColumn.Type),
            }); 
        }

        private void filterCell_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu context = (ContextMenu)((MenuItem)sender).Parent;

            SortGridViewColumnHeader gvch = GetCellColumnHeader(context);

            if (gvch == null)
                return;

            ResultRow row = (ResultRow)lvResult.SelectedItem;
            object value = row[gvch.ResultColumn];

            FilterOptions.Add(new FilterOption
            {
                Token = gvch.RequestColumn.Token,
                Operation =  FilterOperation.EqualTo,
                Value = value is EmbeddedEntity ? null : value
            });
        }

        private SortGridViewColumnHeader GetCellColumnHeader(ContextMenu context)
        {
            Point point = context.PointToScreen(new Point(0, 0));

            Point newPoint = lvResult.PointFromScreen(point);

            Point headerPoint = new Point(newPoint.X, 4);

            HitTestResult hitResult = VisualTreeHelper.HitTest(lvResult, headerPoint);

            if (hitResult == null)
                return null;

            SortGridViewColumnHeader gvch = hitResult.VisualHit.VisualParents().OfType<SortGridViewColumnHeader>().FirstOrDefault();
            return gvch;
        }

        public void Reinitialize(List<FilterOption> filters, List<ColumnOption> columns, ColumnOptionsMode columnOptionsMode, List<OrderOption> orders, Pagination pagination)
        {
            try
            {
                avoidPaginationChange = true;

                ColumnOptions.Clear();
                ColumnOptions.AddRange(columns);
                ColumnOption.SetColumnTokens(ColumnOptions, Description);
                ColumnOptionsMode = columnOptionsMode;
                GenerateListViewColumns();

                if (!filters.SequenceEqual(FilterOptions))
                {
                    if (SimpleFilterBuilder != null)
                        SimpleFilterBuilder = null;

                    FilterOptions.Clear();
                    FilterOption.SetFilterTokens(filters, Description);
                    FilterOptions.AddRange(filters);
                }

                OrderOptions.Clear();
                OrderOptions.AddRange(orders);
                OrderOption.SetOrderTokens(OrderOptions, Description);
                SortGridViewColumnHeader.SetColumnAdorners(gvResults, OrderOptions);

                UpdateMultiplyMessage(true);

                Pagination = pagination;
            }
            finally
            {
                avoidPaginationChange = false;
            }
        }

        private void btFilters_Unchecked(object sender, RoutedEventArgs e)
        {
            rowFilters.Height = new GridLength(); //Auto
        }

        public void FocusSearch()
        {
            Keyboard.Focus(btSearch);
        }

        private void GridSearchControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                Search(true);
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }
    }

    public delegate void ResultChangedEventHandler(object sender, ResultChangedEventArgs e);

    public class ResultChangedEventArgs : RoutedEventArgs
    {
        public bool Cleaning { get; private set; }
        public ResultChangedEventArgs(RoutedEvent routedEvent, bool cleaning)
            : base(routedEvent)
        {
            this.Cleaning = cleaning;
        }
    }

    public interface ISimpleFilterBuilder
    {
        List<FilterOption> GenerateFilterOptions();
    }
}
