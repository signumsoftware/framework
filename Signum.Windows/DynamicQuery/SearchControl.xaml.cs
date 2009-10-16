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
          DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOptions>), typeof(SearchControl), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOptions> FilterOptions
        {
            get { return (FreezableCollection<FilterOptions>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
        }

        public int ItemsCount
        {
            get { return (int) lvResult.Items.Count; }
        }

        public static readonly DependencyProperty FilterVisibleProperty =
        DependencyProperty.Register("FilterVisible", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool FilterVisible
        {
            get { return (bool)GetValue(FilterVisibleProperty); }
            set
            {
                SetValue(FilterVisibleProperty, value);
                if (value == false) { rdFilter.MinHeight = 0; rdFilter.Height = new GridLength(0); }
            }
        }

        public static readonly DependencyProperty FooterVisibleProperty =
        DependencyProperty.Register("FooterVisible", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool FooterVisible
        {
            get { return (bool)GetValue(FooterVisibleProperty); }
            set { SetValue(FooterVisibleProperty, value);
            if (value == false) { rdFooter.MinHeight = 0; rdFooter.Height = new GridLength(0); }
            }
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

        public static readonly DependencyProperty ModeProperty =
                DependencyProperty.Register("Mode", typeof(FilterMode), typeof(SearchControl), new FrameworkPropertyMetadata(FilterMode.Hidden,
        (d, e) => ((SearchControl)d).ModeChanged()));
        public FilterMode Mode
        {
            get { return (FilterMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public static readonly DependencyProperty SearchOnLoadProperty =
          DependencyProperty.Register("SearchOnLoad", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
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
            btCreate.Visibility = Create  && EntityType != null ? Visibility.Visible : Visibility.Collapsed;
            UpdateViewSelection();
        }

        public event Func<object> Creating;
        public event Action<object> Viewing;
        public event Action DoubleClick;

        public SearchControl()
        {
            this.InitializeComponent();

            FilterOptions = new FreezableCollection<FilterOptions>();
            this.Loaded += new RoutedEventHandler(SearchControl_Loaded);
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
                SetValue(EntityTypeKey, Reflector.ExtractLazy(entity.Type));

                if (this.NotSet(ViewProperty) && View)
                    View = Navigator.IsViewable(EntityType, true);

                if (this.NotSet(CreateProperty) && Create)
                    Create = Navigator.IsCreable(EntityType, true);
            }

            foreach (var fo in FilterOptions)
            {
                fo.Column = view.Columns.Where(c => c.Name == fo.ColumnName)
                    .Single(Properties.Resources.Column0NotFoundOnQuery1.Formato(fo.ColumnName, QueryName));
                fo.RefreshRealValue();
            }

            filterBuilder.Columns = view.Columns.Where(a => a.Filterable).ToList();
            filterBuilder.Filters = new ObservableCollection<FilterOptions>(FilterOptions);

            GenerateListViewColumns(view);

            ModeChanged();

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
                Search();

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

        private void GenerateListViewColumns(QueryDescription view)
        {
            gvResults.Columns.Clear();

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
                Navigator.View(new ViewOptions { Buttons = ViewButtons.Save, Admin = true }, entity);
            else
                this.Viewing(entity);
        }


        private void lvResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateViewSelection();
        }


        private void expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (!ignoreExpanded)
            {
                Mode = expander.IsExpanded ? (expander.IsEnabled ? FilterMode.Visible : FilterMode.VisibleAndReadOnly) :
                       (expander.IsEnabled ? FilterMode.Visible : FilterMode.VisibleAndReadOnly);
            }
        }

        bool ignoreExpanded = false;

        private void ModeChanged()
        {
            expander.IsEnabled = Mode == FilterMode.Hidden || Mode == FilterMode.Visible;
            try
            {
                ignoreExpanded = true;
                expander.IsExpanded = Mode == FilterMode.VisibleAndReadOnly || Mode == FilterMode.Visible;
            }
            finally
            {
                ignoreExpanded = false;
            }
        }

        private void lvResult_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DoubleClick != null)
                DoubleClick();
            else
                OnViewClicked();
            e.Handled = true;
        }

        void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
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

            string header = headerClicked.Column.Header as string;
            int colIndex = queryResult.Columns.IndexOf(d => d.DisplayName == header);

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
                SearchControl = this.Parents().OfType<SearchControl>().First();

                SearchControl.QueryResultChanged += new RoutedEventHandler(searchControl_QueryResultChanged);

                Initialize();
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
