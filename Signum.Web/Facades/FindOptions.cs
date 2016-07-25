using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Engine;
using Signum.Utilities.DataStructures;
using Signum.Engine.DynamicQuery;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Signum.Web
{
    public class CountOptions
    {
        public object QueryName { get; set; }

        List<FilterOption> filterOptions = new List<FilterOption>();
        public List<FilterOption> FilterOptions
        {
            get { return filterOptions; }
            set { this.filterOptions = value; }
        }

        public CountOptions() { }
        public CountOptions(object queryName)
        {
            this.QueryName = queryName;
        }
    }

    [JsonConverter(typeof(FindOptions.FindOptionsConverter))]
    public class FindOptions
    {
        public object QueryName { get; set; }

        List<FilterOption> filterOptions = new List<FilterOption>();
        public List<FilterOption> FilterOptions
        {
            get { return filterOptions; }
            set { this.filterOptions = value; }
        }

        List<OrderOption> orderOptions = new List<OrderOption>();
        public List<OrderOption> OrderOptions
        {
            get { return orderOptions; }
            set { this.orderOptions = value; }
        }

        ColumnOptionsMode columnOptionsMode = ColumnOptionsMode.Add;
        public ColumnOptionsMode ColumnOptionsMode
        {
            get { return columnOptionsMode; }
            set { this.columnOptionsMode = value; }
        }

        List<ColumnOption> columnOptions = new List<ColumnOption>();
        public List<ColumnOption> ColumnOptions
        {
            get { return columnOptions; }
            set { this.columnOptions = value; }
        }

        public static Pagination DefaultPagination = new Pagination.Paginate(20, 1);
        public static Func<object, Pagination, Pagination> ReplacePagination = (queryKey, pagination) => pagination;

        Pagination pagination;
        public Pagination Pagination
        {
            get { return pagination; }
            set { pagination = value; }
        }

        public FindOptions() { }

        public FindOptions(object queryName)
        {
            this.QueryName = queryName;
        }

        public FindOptions(object queryName, string parentColumn, object parentValue)
        {
            this.QueryName = queryName;
            this.FilterOptions.Add(new FilterOption(parentColumn, parentValue) { Frozen = true });

            if (QueryUtils.IsColumnToken(parentColumn))
            {
                this.ColumnOptionsMode = Signum.Entities.DynamicQuery.ColumnOptionsMode.Remove;
                this.ColumnOptions.Add(new ColumnOption(parentColumn));
            }
            this.SearchOnLoad = true;
            this.ShowFilters = false;
        }

        public FindOptions RemovePagination()
        {
            this.ShowFooter = false;
            this.Pagination = new Pagination.All();
            return this;
        }

        bool? allowChangeColumns;
        public bool AllowChangeColumns
        {
            get { return allowChangeColumns ?? Finder.Manager.AllowChangeColumns(); }
            set { allowChangeColumns = value; }
        }

        public bool SearchOnLoad { get; set; }

        bool allowSelection = true;
        public bool AllowSelection
        {
            get { return allowSelection; }
            set { allowSelection = value; }
        }

        bool? allowOrder ;
        public bool AllowOrder
        {
            get { return allowOrder ?? Finder.Manager.AllowOrder(); }
            set { allowOrder = value; }
        }

        bool showHeader = true;
        public bool ShowHeader
        {
            get { return showHeader; }
            set { showHeader = value; }
        }

        bool showFilters = true;
        public bool ShowFilters
        {
            get { return showFilters; }
            set { showFilters = value; }
        }

        bool showFilterButton = true;
        public bool ShowFilterButton
        {
            get { return showFilterButton; }
            set { showFilterButton = value; }
        }

        bool showFooter = true;
        public bool ShowFooter
        {
            get { return showFooter; }
            set { showFooter = value; }
        }

        bool showContextMenu = true;
        public bool ShowContextMenu
        {
            get { return showContextMenu; }
            set { showContextMenu = value; }
        }

        bool create = true;
        public bool Create
        {
            get { return create; }
            set { create = value; }
        }

        bool navigate = true;
        public bool Navigate
        {
            get { return navigate; }
            set { navigate = value; }
        }

        bool selectedItemsContextMenu = ContextualItemsHelper.SelectedItemsMenuInSearchPage;
        public bool SelectedItemsContextMenu
        {
            get { return selectedItemsContextMenu; }
            set { selectedItemsContextMenu = value; }
        }

        public override string ToString()
        {
            if (FilterOptions.Any())
            {
                QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(QueryName);

                FilterOption.SetFilterTokens(FilterOptions, queryDescription, canAggregate: false);
            }

            var elements = Pagination != null ? Pagination.GetElementsPerPage() : null;

            string options = new Sequence<string>
            {
                Pagination != null ? "pagination=" + pagination.GetMode().ToString() : null,
                elements != null ? "elems=" + elements : null,
                SearchOnLoad ? "searchOnLoad=true" : null,
                !Create ? "create=false": null, 
                !Navigate ? "navigate=false": null, 
                !AllowSelection ? "allowSelection=false" : null,
                !AllowChangeColumns ? "allowChangeColumns=false" : null,
                !AllowOrder ? "allowOrder=false" : null,
                !showHeader ? "showHeader=false" : null,
                !showFilters ? "showFilters=false" : null,
                !showFilterButton ? "showFilterButton=false" : null,
                !showFooter ? "showFooter=false" : null,
                !showContextMenu ? "showContextMenu=false" : null,
                (FilterOptions != null && FilterOptions.Count > 0) ? ("filters=" + FilterOptions.ToString(";")) : null,
                (OrderOptions != null && OrderOptions.Count > 0) ? ("orders=" + OrderOptions.ToString(";")) : null,
                (ColumnOptions != null && ColumnOptions.Count > 0) ? ("columns=" + ColumnOptions.ToString(";")) : null, 
                (ColumnOptionsMode != ColumnOptionsMode.Add ? ("columnMode=" + ColumnOptionsMode.ToString()) : null),
                !SelectedItemsContextMenu ? "selectedItemsContextMenu=false" : null 
            }.NotNull().ToString("&");

            if (options.HasText())
                return Finder.FindRoute(QueryName) + "?" + options;
            else
                return Finder.FindRoute(QueryName);
        }

        public JObject ToJS(string parentPrefix, string newPart)
        {
            return ToJS(TypeContextUtilities.Compose(parentPrefix, newPart));
        }

        public JObject ToJS(string prefix)
        {
            JObject op = new JObject() { { "prefix", prefix } };

            if (FilterOptions.Any())
            {
                QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(QueryName);

                FilterOption.SetFilterTokens(this.FilterOptions, queryDescription, false);
            }

            if (QueryName != null) op.Add("webQueryName", Finder.ResolveWebQueryName(QueryName));
            if (SearchOnLoad == true) op.Add("searchOnLoad", true);
            if (!Navigate) op.Add("navigate", false);
            if (!Create) op.Add("create", false);
            if (!AllowSelection) op.Add("allowSelection", false);
            if (!SelectedItemsContextMenu) op.Add("selectedItemsContextMenu", false);
            if (!AllowChangeColumns) op.Add("allowChangeColumns", false);
            if (!AllowOrder) op.Add("allowOrder", false);
            if (!showHeader) op.Add("showHeader", false);
            if (!showFilters) op.Add("showFilters", false);
            if (!showFilterButton) op.Add("showFilterButton", false);
            if (!showFooter) op.Add("showFooter", false);
            if (!showContextMenu) op.Add("showContextMenu", false);
            if (FilterOptions.Any()) op.Add("filters", new JArray(filterOptions.Select(f => f.ToJS())));
            if (OrderOptions.Any()) op.Add("orders", new JArray(OrderOptions.Select(oo => oo.ToJS()  )));
            if (ColumnOptions.Any()) op.Add("columns", new JArray(ColumnOptions.Select(co => co.ToJS() )));
            if (ColumnOptionsMode != Entities.DynamicQuery.ColumnOptionsMode.Add) op.Add("columnMode", ColumnOptionsMode.ToString());

            if (Pagination != null)
            {
                op.Add("pagination", Pagination.GetMode().ToString());
                int? elems = Pagination.GetElementsPerPage();
                if (elems != null)
                    op.Add("elems", elems.Value.ToString());
            }

            return op;
        }

        public QueryRequest ToQueryRequest()
        {
            var qd = DynamicQueryManager.Current.QueryDescription(QueryName);
            return new QueryRequest
            {
                QueryName = QueryName,
                Filters = FilterOptions.Select(fo => fo.ToFilter()).ToList(),
                Orders = OrderOptions.Select(fo => fo.ToOrder()).ToList(),
                Columns = ColumnOptions.Select(co => co.ToColumn(qd)).ToList(),
                Pagination = pagination ?? FindOptions.DefaultPagination,
            };
        }

        public List<Column> MergeColumns()
        {
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(QueryName);

            switch (ColumnOptionsMode)
            {
                case ColumnOptionsMode.Add:
                    return qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd, qd.QueryName)).Concat(ColumnOptions.Select(co => co.ToColumn(qd))).ToList();
                case ColumnOptionsMode.Remove:
                    return qd.Columns.Where(cd => !cd.IsEntity && !ColumnOptions.Any(co => co.ColumnName == cd.Name)).Select(cd => new Column(cd, qd.QueryName)).ToList();
                case ColumnOptionsMode.Replace:
                    return ColumnOptions.Select(co => co.ToColumn(qd)).ToList();
                default:
                    throw new InvalidOperationException("{0} is not a valid ColumnOptionMode".FormatWith(ColumnOptionsMode));
            }
        }


        class FindOptionsConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(FindOptions).IsAssignableFrom(objectType); 
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException("in order to serialize FindOptions call FindOptions.ToJs()");
            }
        }
     
    }

    public class UniqueOptions
    {
        public object QueryName { get; set; }

        List<FilterOption> filterOptions = new List<FilterOption>();
        public List<FilterOption> FilterOptions
        {
            get { return filterOptions; }
            set { this.filterOptions = value; }
        }

        List<OrderOption> orderOptions = new List<OrderOption>();
        public List<OrderOption> OrderOptions
        {
            get { return orderOptions; }
            set { this.orderOptions = value; }
        }

        public UniqueOptions()
        {
            UniqueType = UniqueType.Single;
        }

        public UniqueOptions(object queryName)
        {
            UniqueType = UniqueType.Single;
            QueryName = queryName;
        }

        public UniqueType UniqueType { get; set; }
    }

    public class FilterOption
    {
        public QueryToken Token { get; set; }
        public string ColumnName { get; set; }
        public bool Frozen { get; set; }
        public FilterOperation Operation { get; set; }
        public object Value { get; set; }

        public FilterOption() { }

        public FilterOption(string columnName, object value)
        {
            this.ColumnName = columnName;
            this.Operation = FilterOperation.EqualTo;
            this.Value = value;         
        }

        public Filter ToFilter()
        {
            Filter f = new Filter(Token, Operation, Common.Convert(Value, Token.Type));

            return f;
        }


        public override string ToString()
        {
            return "{0},{1},{2}".FormatWith(ColumnName, Operation.ToString(), EncodeCSV(StringValue()));
        }

        public string StringValue()
        {
            object v = Common.Convert(Value, Token.Type);
            if (v == null)
                return null;

            if (v.GetType().IsLite())
            {
                Lite<Entity> lite = (Lite<Entity>)v;
                return lite.Key();
            }
            
            return v.ToString();
        }

        string EncodeCSV(string p)
        {
            if (p == null)
                return null;

            bool hasQuote = p.Contains("\"");
            if (hasQuote || p.Contains(",") || p.Contains(";"))
            {
                if (hasQuote)
                    p = p.Replace("\"", "\"\"");
                return "\"" + p + "\"";
            }
            return p;
        }

        public JObject ToJS()
        {
            return new JObject { { "columnName", ColumnName }, { "operation", (int)Operation }, { "value", StringValue() } };
        }

        public static void SetFilterTokens(List<FilterOption> filters, QueryDescription queryDescription, bool canAggregate)
        {
            foreach (var f in filters.Where(f => f.ColumnName.HasText() && f.Token == null))
                f.Token = QueryUtils.Parse(f.ColumnName, queryDescription, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));
        }
    }

    public class OrderOption
    {
        public OrderOption()
        {
        }

        public OrderOption(string columnName)
        {
            this.ColumnName = columnName;
        }

        public OrderOption(string columnName, OrderType orderType)
        {
            this.ColumnName = columnName;
            this.OrderType = orderType;
        }

        public QueryToken Token { get; set; }
        public string ColumnName { get; set; }
        public OrderType OrderType { get; set; }

        public Order ToOrder()
        {
            return new Order(Token, OrderType);
        }

        public override string ToString()
        {
            return (OrderType == OrderType.Descending ? "-" : "") + ColumnName;
        }

        public JObject ToJS()
        {
            return new JObject { { "columnName", ColumnName }, { "orderType", (int)OrderType } };
        }

        public static void SetOrderTokens(List<OrderOption> orders, QueryDescription queryDescription, bool canAggregate)
        {
            foreach (var o in orders.Where(o => o.ColumnName.HasText() && o.Token == null))
                o.Token = QueryUtils.Parse(o.ColumnName, queryDescription, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));
        }
    }

    public class ColumnOption
    {
        public ColumnOption()
        {
        }

        public ColumnOption(string columnName)
        {
            this.ColumnName = columnName;
        }

        public QueryToken Token { get; set; }
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }

        public Column ToColumn(QueryDescription qd, bool isVisible = true)
        {
            return new Column(Token, DisplayName.DefaultText(Token.NiceName())) { IsVisible = isVisible };
        }

        public override string ToString()
        {
            return DisplayName.HasText() ? "{0},{1}".FormatWith(ColumnName, DisplayName) : ColumnName;
        }

        public JObject ToJS()
        {
            return new JObject { { "columnName", ColumnName }, { "displayName", DisplayName } };
        }

        public static void SetColumnTokens(List<ColumnOption> columns, QueryDescription queryDescription, bool canAggregate)
        {
            foreach (var o in columns.Where(c => c.ColumnName.HasText() && c.Token == null))
                o.Token = QueryUtils.Parse(o.ColumnName, queryDescription, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));
        }
    }

    public enum FindMode
    {
        Find,
        Explore
    }
}
