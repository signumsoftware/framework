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

namespace Signum.Windows
{
    public partial class SearchControl
    {
        private GridViewColumnHeader lastHeaderClicked = null;
        private ListSortDirection lastDirection = ListSortDirection.Ascending;

        public static readonly DependencyProperty QueryNameProperty =
            DependencyProperty.Register("QueryName", typeof(object), typeof(SearchControl), new UIPropertyMetadata(null));
        public object QueryName
        {
            get { return (object)GetValue(QueryNameProperty); }
            set { SetValue(QueryNameProperty, value); }
        }

        public static readonly DependencyProperty FilterOptionsProperty =
          DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOption>), typeof(SearchControl), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> FilterOptions
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
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
          DependencyProperty.Register("SelectedItem", typeof(object), typeof(SearchControl), new UIPropertyMetadata(null));
        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }


        public static readonly DependencyProperty SelectedItemsProperty =
          DependencyProperty.Register("SelectedItems", typeof(object[]), typeof(SearchControl), new UIPropertyMetadata(null));
        public object[] SelectedItems
        {
            get { return (object[])GetValue(SelectedItemsProperty); }
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
            this.Loaded += new RoutedEventHandler(SearchControl_Loaded);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick +=new EventHandler(timer_Tick);
        }

        int entityIndex;
        QueryResult queryResult;
        public QueryResult QueryResult { get { return queryResult; } }

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

            QueryDescription view = Server.Service<IQueryServer>().GetQueryDescription(QueryName);

            Column entity = view.Columns.SingleOrDefault(a => a.IsEntity);
            if (entity != null)
            {
                entityIndex = view.Columns.IndexOf(entity);
                SetValue(EntityTypeKey, Reflector.ExtractLite(entity.Type));

                if (this.NotSet(ViewProperty) && View)
                    View = Navigator.IsViewable(EntityType, true);

                if (this.NotSet(CreateProperty) && Create)
                    Create = Navigator.IsCreable(EntityType, true);
            }

            foreach (var fo in FilterOptions)
            {
                fo.Column = view.Columns.Where(c => c.Name == fo.ColumnName)
                    .Single(Properties.Resources.Column0NotFoundOnQuery1.Formato(fo.ColumnName, QueryName));
                fo.ValueChanged += new EventHandler(fo_ValueChanged);
                fo.RefreshRealValue();
            }

            filterBuilder.Columns = view.Columns.Where(a => a.Filterable).ToList();
            filterBuilder.Filters = new ObservableCollection<FilterOption>(FilterOptions);

            GenerateListViewColumns(view);
            columns = view.Columns;

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
            if (queryResult != null)
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

            SelectedItem = ((object[])lvResult.SelectedItem).TryCC(a => a[entityIndex]);
            if (MultiSelection)
                SelectedItems = lvResult.SelectedItems.Cast<object[]>().Select(a => a[entityIndex]).ToArray();
            else
                SelectedItems = null;
        }

        List<Column> columns = new List<Column>();
        List<Column> visibleColumns = new List<Column>();

        private void GenerateListViewColumns(QueryDescription view)
        {
            gvResults.Columns.Clear();
            visibleColumns.Clear();

            for (int i = 0; i < view.Columns.Count; i++)
            {
                Column c = view.Columns[i];

                if (!c.Visible)
                    continue;

                Binding b = new Binding("[{0}]".Formato(i)) { Mode = BindingMode.OneTime };
                DataTemplate dt = settings.GetFormatter(c)(b);
                gvResults.Columns.Add(
                    new GridViewColumn
                    {
                        Header = c.DisplayName,
                        CellTemplate = dt,
                    });

                visibleColumns.Add(c);
            }
        }

        private void FilterBuilder_SearchClicked(object sender, RoutedEventArgs e)
        {
            Search();
        }

        public List<Filter> CurrentFilters()
        {
            return filterBuilder.Filters.Select(f => f.ToFilter()).ToList();
        }

        public void Search()
        {
            btFind.IsEnabled = false;

            queryResult = null;

            OnQueryResultChanged(true);

            object vn = QueryName;
            var lf = CurrentFilters();
            tbResultados.Visibility = Visibility.Hidden;

            int? limit = tbLimite.Text.ToInt();

            Async.Do(this.FindCurrentWindow(),
                () => queryResult = Server.Service<IQueryServer>().GetQueryResult(vn, lf, limit),
                () =>
                {
                    if (queryResult != null)
                    {
                        lvResult.ItemsSource = queryResult.Data;
                        if (queryResult.Data.Length > 0)
                        {
                            lvResult.SelectedIndex = 0;
                            lvResult.ScrollIntoView(queryResult.Data.First());
                        }
                        ItemsCount = lvResult.Items.Count;
                        lvResult.Background = Brushes.White;
                        lvResult.Focus();
                        tbResultados.Visibility = Visibility.Visible;
                        tbResultados.Foreground = queryResult.Data.Length == limit ? Brushes.Red : Brushes.Black;
                        OnQueryResultChanged(false);
                    }

                },
                () => { btFind.IsEnabled = true; });
        }

        private void OnQueryResultChanged(bool cleaning)
        {
            if (!cleaning && CollapseOnNoResults)
                Visibility = queryResult.Data.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

            RaiseEvent(new RoutedEventArgs(QueryResultChangedEvent));
        }

        private void btView_Click(object sender, RoutedEventArgs e)
        {
            OnViewClicked();
        }

        private void OnViewClicked()
        {
            object[] row = (object[])lvResult.SelectedItem;

            if (row == null)
                return;

            object entity = row[entityIndex];

            OnViewing(entity);
        }

        private void btCreate_Click(object sender, RoutedEventArgs e)
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
                Navigator.View(entity, new ViewOptions { Buttons = ViewButtons.Save, ReadOnly = Navigator.IsReadOnly(EntityType, true) });
            else
                this.Viewing(entity);
        }


        private void lvResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateViewSelection();
        }

        //bool ignoreExpanded = false;

        //private void FilterModeChanged()
        //{
        //    expander.IsEnabled = FilterMode == FilterMode.Hidden || FilterMode == FilterMode.Visible;
        //    try
        //    {
        //        ignoreExpanded = true;
        //        expander.IsExpanded = FilterMode == FilterMode.VisibleAndReadOnly || FilterMode == FilterMode.Visible;
        //    }
        //    finally
        //    {
        //        ignoreExpanded = false;
        //    }
        //}

        private void lvResult_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DoubleClick != null)
                DoubleClick();
            else
                OnViewClicked();
            e.Handled = true;
        }

        void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (queryResult == null || queryResult.Data == null)
                return;

            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Role == GridViewColumnHeaderRole.Padding)
                return;

            ListSortDirection direction;
            if (headerClicked != lastHeaderClicked)
                direction = ListSortDirection.Ascending;
            else
            {
                if (lastDirection == ListSortDirection.Ascending)
                    direction = ListSortDirection.Descending;
                else
                    direction = ListSortDirection.Ascending;
            }


            int columHeaderIndex = gvResults.Columns.IndexOf(headerClicked.Column);
            int colIndex = columns.IndexOf(visibleColumns[columHeaderIndex]);

            btFind.IsEnabled = false;

            Async.Do(this.FindCurrentWindow(),
                   () => queryResult.Data = SortResults(colIndex, direction),
                   () => { lvResult.ItemsSource = queryResult.Data; lvResult.SelectedIndex = 0; lvResult.Focus(); },
                   () => { btFind.IsEnabled = true; });

            lastHeaderClicked = headerClicked;
            lastDirection = direction;
        }

        private object[][] SortResults(int colIndex, ListSortDirection direccion)
        {
            Type type = queryResult.Columns[colIndex].Type;

            Func<object[], IComparable> lambda = null;

            if (typeof(IComparable).IsAssignableFrom(type.UnNullify()))
                lambda = arr => (IComparable)arr[colIndex];
            else
                lambda = arr => arr[colIndex].TryCC(a => a.ToString());

            if (direccion == ListSortDirection.Ascending)
                return queryResult.Data.OrderBy(lambda).ToArray();
            else
                return queryResult.Data.OrderByDescending(lambda).ToArray();
        }


        public static event MenuItemForQueryName GetCustomMenuItems;

        private void GridViewColumnHeader_PreviewMouseMove(object sender, MouseEventArgs e)
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

        private FilterOption CreateFilter(GridViewColumnHeader header)
        {
            int columnHeaderIndex = gvResults.Columns.IndexOf(header.Column);
            Column column = visibleColumns[columnHeaderIndex];

            if (queryResult != null)
            {
                int columnIndex = columns.IndexOf(column);

                object[] row = (object[])lvResult.SelectedItem;
                if (row != null)
                {
                    return new FilterOption
                    {
                        Column = column,
                        Operation = FilterOperation.EqualTo,
                        Value = row[columnIndex]
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

        private void GridViewColumnHeader_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void GridViewColumnHeader_MouseLeave(object sender, MouseEventArgs e)
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
