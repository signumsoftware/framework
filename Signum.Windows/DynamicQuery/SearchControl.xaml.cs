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

        public static readonly DependencyProperty MaxItemsCountProperty =
            DependencyProperty.Register("MaxItemsCount", typeof(int?), typeof(SearchControl), new UIPropertyMetadata(200));
        public int? MaxItemsCount
        {
            get { return (int?)GetValue(MaxItemsCountProperty); }
            set { SetValue(MaxItemsCountProperty, value); }
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

        public static readonly DependencyProperty ViewReadOnlyProperty =
            DependencyProperty.Register("ViewReadOnly", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool ViewReadOnly
        {
            get { return (bool)GetValue(ViewReadOnlyProperty); }
            set { SetValue(ViewReadOnlyProperty, value); }
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

        private static readonly DependencyPropertyKey EntityTypeKey =
         DependencyProperty.RegisterReadOnly("EntityType", typeof(Type), typeof(SearchControl), new UIPropertyMetadata(null));
        public static readonly DependencyProperty EntityTypeProperty = EntityTypeKey.DependencyProperty;
        public Type EntityType
        {
            get { return (Type)GetValue(EntityTypeProperty); }
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

        public event Func<object> Creating;
        public event Action<object> Viewing;
        public event Action DoubleClick;

        public SearchControl()
        {
            this.InitializeComponent();

            FilterOptions = new FreezableCollection<FilterOption>();
            OrderOptions = new ObservableCollection<OrderOption>();
            this.Loaded += new RoutedEventHandler(SearchControl_Loaded);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(timer_Tick);
        }

        Column entityColumn;
        ResultTable resultTable;
        public ResultTable ResultTable { get { return resultTable; } }

        public static readonly RoutedEvent QueryResultChangedEvent = EventManager.RegisterRoutedEvent(
            "QueryResultChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchControl));
        public event RoutedEventHandler QueryResultChanged
        {
            add { AddHandler(QueryResultChangedEvent, value); }
            remove { RemoveHandler(QueryResultChangedEvent, value); }
        }

        QuerySettings settings;

        void SearchControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SearchControl_Loaded;

            if (DesignerProperties.GetIsInDesignMode(this) || QueryName == null)
                return;

            settings = Navigator.GetQuerySettings(QueryName);

            QueryDescription view = Navigator.Manager.GetQueryDescription(QueryName);

            entityColumn = view.Columns.SingleOrDefault(a => a.IsEntity);
            if (entityColumn != null)
            {
                SetValue(EntityTypeKey, Reflector.ExtractLite(entityColumn.Type));

                if (this.NotSet(ViewProperty) && View && view.EntityImplementations == null)
                    View = Navigator.IsViewable(EntityType, IsAdmin);

                if (this.NotSet(CreateProperty) && Create && view.EntityImplementations == null)
                    Create = Navigator.IsCreable(EntityType, IsAdmin);

                if (this.NotSet(ViewReadOnlyProperty) && !ViewReadOnly && view.EntityImplementations == null)
                    ViewReadOnly = Navigator.IsReadOnly(EntityType, IsAdmin);
            }

            Navigator.Manager.SetColumns(QueryName, FilterOptions);

            foreach (var fo in FilterOptions)
            {
                fo.ValueChanged += new EventHandler(fo_ValueChanged);
            }

            filterBuilder.Columns = view.Columns.Where(a => a.Filterable).ToList();
            filterBuilder.Filters = new ObservableCollection<FilterOption>(FilterOptions);

            Navigator.Manager.SetColumns(QueryName, OrderOptions); 

            GenerateListViewColumns(view);

            adorners = OrderOptions.Select((o, i) => new ColumnOrderInfo(
                gvResults.Columns.Select(c => (GridViewColumnHeader)c.Header).Single(c => ((Column)c.Tag).Name == o.ColumnName),
                o.OrderType,
                i + 1)).ToDictionary(a => ((Column)((GridViewColumnHeader)a.Header).Tag).Name);


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

        private void UpdateViewSelection()
        {
            btView.Visibility = lvResult.SelectedItem != null && View ? Visibility.Visible : Visibility.Collapsed;

            SelectedItem = ((ResultRow)lvResult.SelectedItem).TryCC(a => (Lite)a[entityColumn]);
            if (MultiSelection)
                SelectedItems = lvResult.SelectedItems.Cast<ResultRow>().Select(a => (Lite)a[entityColumn]).ToArray();
            else
                SelectedItems = null;
        }

        private void GenerateListViewColumns(QueryDescription view)
        {
            gvResults.Columns.Clear();

            foreach (var c in view.Columns.Where(c=>c.Visible))
            {
                Binding b = new Binding("[{0}]".Formato(c.Index)) { Mode = BindingMode.OneTime };
                DataTemplate dt = settings.GetFormatter(c)(b);
                gvResults.Columns.Add(
                    new GridViewColumn
                    {
                        Header = new GridViewColumnHeader { Content = c.DisplayName, Tag = c },
                        CellTemplate = dt,
                    });
            }
        }

        private void FilterBuilder_SearchClicked(object sender, RoutedEventArgs e)
        {
            Search();
        }

        public void Search()
        {
            btFind.IsEnabled = false;

            resultTable = null;

            OnQueryResultChanged(true);

            object vn = QueryName;
            var filters = CurrentFilters();
            var orders = CurrentOrders();
            tbResultados.Visibility = Visibility.Hidden;

            int? limit = MaxItemsCount;

            Async.Do(this.FindCurrentWindow(),
                () => resultTable = Server.Return((IQueryServer s) => s.GetQueryResult(vn, filters, orders, limit)),
                () =>
                {
                    if (resultTable != null)
                    {
                        lvResult.ItemsSource = resultTable.Rows;
                        if (resultTable.Rows.Length > 0)
                        {
                            lvResult.SelectedIndex = 0;
                            lvResult.ScrollIntoView(resultTable.Rows.First());
                        }
                        ItemsCount = lvResult.Items.Count;
                        lvResult.Background = Brushes.White;
                        lvResult.Focus();
                        tbResultados.Visibility = Visibility.Visible;
                        tbResultados.Foreground = resultTable.Rows.Length == limit ? Brushes.Red : Brushes.Black;
                        OnQueryResultChanged(false);
                    }

                },
                () => { btFind.IsEnabled = true; });
        }

        public List<Order> CurrentOrders()
        {
            var orders = adorners.Values.Select(a => a.ToOrder()).ToList();
            return orders;
        }

        public List<Filter> CurrentFilters()
        {
            var filters = filterBuilder.Filters.Select(f => f.ToFilter()).ToList();
            return filters;
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

            object entity = row[entityColumn];

            OnViewing(entity);
        }

        void btCreate_Click(object sender, RoutedEventArgs e)
        {
            OnCreate();
        }

        protected void OnCreate()
        {
            if (!Create)
                return;

            object result = Creating == null ? Constructor.Construct(EntityType, this.FindCurrentWindow()) : Creating();

            if (result == null)
                return;

            if (ViewOnCreate)
            {
                OnViewing(result);
            }
        }

        protected void OnViewing(object entity)
        {
            if (!View)
                return;

            if (this.Viewing == null)
            {
                Navigator.NavigateUntyped(entity, new NavigateOptions { ReadOnly = ViewReadOnly });
            }
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


        Dictionary<string, ColumnOrderInfo> adorners = new Dictionary<string, ColumnOrderInfo>();
      
        void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader header = sender as GridViewColumnHeader;
            Column column = header.Tag as Column;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift || (adorners.Count == 1 && adorners.ContainsKey(column.Name)))
            {

            }
            else
            {
                foreach (var item in adorners.Values)
                    item.Clean();

                adorners.Clear();
            }

            ColumnOrderInfo orderOption;
            if (adorners.TryGetValue(column.Name, out orderOption))
            {
                orderOption.Flip();
            }
            else
            {
                adorners.Add(column.Name, new ColumnOrderInfo(header, OrderType.Ascending, adorners.Count + 1));
            }

            Search();
        }

        public static event MenuItemForQueryName GetCustomMenuItems;

        void GridViewColumnHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && this.ShowFilters && _startPoint != null)
            {
                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.Value.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _startPoint.Value.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _startPoint = null;
                    GridViewColumnHeader header = (GridViewColumnHeader)sender;
                    FilterOption filter = CreateFilter(header);

                    if (DragDrop.DoDragDrop(header, filter, DragDropEffects.Copy) == DragDropEffects.Copy)
                    {

                    }

                    e.Handled = true;
                }
            }
        }

        FilterOption CreateFilter(GridViewColumnHeader header)
        {
            Column column = (Column)header.Tag;

            if (resultTable != null)
            {
                ResultRow row = (ResultRow)lvResult.SelectedItem;
                if (row != null)
                {
                    return new FilterOption
                    {
                        Column = column,
                        Operation = FilterOperation.EqualTo,
                        Value = row[column]
                    };
                }
            }

            return new FilterOption
            {
                Column = column,
                Operation = FilterOperation.EqualTo,
                Value = FilterOption.DefaultValue(column.Type),
            };
        }

        Point? _startPoint;

        void GridViewColumnHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        void GridViewColumnHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            _startPoint = null;
        }
    }

    public delegate MenuItem MenuItemForQueryName(object queryName, Type entityType);

    public class SearchControlMenuItem : MenuItem
    {
        protected SearchControl SearchControl;

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
                // TODO: olmo el error cansino de los buscadore al iniciar
                var result = this.Parents().OfType<SearchControl>().FirstOrDefault();
                if (result != null)
                {
                    SearchControl = result;

                    SearchControl.QueryResultChanged += new RoutedEventHandler(searchControl_QueryResultChanged);

                    Initialize();
                }

            }
        }

        void searchControl_QueryResultChanged(object sender, RoutedEventArgs e)
        {
            QueryResultChanged();
        }

        protected virtual void Initialize()
        {

        }

        protected virtual void QueryResultChanged()
        {

        }
    }
}
