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

        public static int DefaultElementsPerPage = 50;


        int? elementsPerPage;
        /// <summary>
        /// If null, use QuerySettings one
        /// </summary>
        public int? ElementsPerPage
        {
            get { return elementsPerPage; }
            set { elementsPerPage = value; }
        }

        public FindOptions() { }

        public FindOptions(object queryName)
        {
            this.QueryName = queryName;
        }

        public FindOptions(object queryName, string parentColumn, object parentValue)
        {
            this.QueryName = queryName;
            this.FilterOptions.Add(new FilterOption(parentColumn, parentValue));
            this.ColumnOptionsMode = Signum.Entities.DynamicQuery.ColumnOptionsMode.Remove;
            this.ColumnOptions.Add(new ColumnOption(parentColumn));
            this.SearchOnLoad = true;
            this.FilterMode = FilterMode.Hidden;
        }

        bool? allowChangeColumns;
        public bool AllowChangeColumns
        {
            get { return allowChangeColumns ?? Navigator.Manager.AllowChangeColumns(); }
            set { allowChangeColumns = value; }
        }

        public bool SearchOnLoad { get; set; }

        bool allowMultiple = true;
        public bool AllowMultiple 
        {
            get { return allowMultiple; }
            set { allowMultiple = value; }
        }
        
        FilterMode filterMode = FilterMode.Visible;
        public FilterMode FilterMode
        {
            get { return filterMode; }
            set 
            { 
                this.filterMode = value;
                if (value == FilterMode.OnlyResults)
                {
                    SearchOnLoad = true;
                    AllowMultiple = false;
                }
            }
        }

        bool create = true;
        public bool Create
        {
            get { return create; }
            set { create = value; }
        }

        public string Creating { get; set; }

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

                Navigator.SetTokens(FilterOptions, queryDescription, canAggregate: false);
            }

            string options = new Sequence<string>
            {
                ElementsPerPage.HasValue ? "elems=" + ElementsPerPage.Value : null,
                SearchOnLoad ? "searchOnLoad=true" : null,
                !Create ? "create=false": null, 
                !Navigate ? "navigate=false": null, 
                !AllowMultiple ? "allowMultiple=false" : null,
                !AllowChangeColumns ? "allowChangeColumns=false" : null,
                FilterMode != FilterMode.Visible ? "filterMode=" + FilterMode.ToString() : null,
                (FilterOptions != null && FilterOptions.Count > 0) ? ("filters=" + FilterOptions.ToString(";") + ";") : null,
                (OrderOptions != null && OrderOptions.Count > 0) ? ("orders=" + OrderOptions.ToString(";") + ";") : null,
                (ColumnOptions != null && ColumnOptions.Count > 0) ? ("columns=" + ColumnOptions.ToString(";") + ";") : null, 
                (ColumnOptionsMode != ColumnOptionsMode.Add ? ("columnMode=" + ColumnOptionsMode.ToString()) : null),
                !SelectedItemsContextMenu ? "selectedItemsContextMenu=false" : null 
            }.NotNull().ToString("&");

            if (options.HasText())
                return Navigator.FindRoute(QueryName) + "?" + options;
            else
                return Navigator.FindRoute(QueryName); 
        }

        public void Fill(JsOptionsBuilder op)
        {
            if (FilterOptions.Any())
            {
                QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(QueryName);

                Navigator.SetTokens(this.FilterOptions, queryDescription, false);
            }

            op.Add("webQueryName", QueryName.TryCC(qn => Navigator.ResolveWebQueryName(qn).SingleQuote()));
            op.Add("searchOnLoad", SearchOnLoad == true ? "true" : null);
            op.Add("filterMode", FilterMode != FilterMode.Visible ? FilterMode.ToString().SingleQuote() : null);
            op.Add("view", !Navigate ? "false" : null);
            op.Add("create", !Create ? "false" : null);
            op.Add("allowMultiple", !AllowMultiple ? "false" : null);
            op.Add("selectedItemsContextMenu", !SelectedItemsContextMenu ? "false" : null);
            op.Add("allowChangeColumns", !AllowChangeColumns ? "false" : null);
            op.Add("filters", filterOptions.IsEmpty() ? null : (filterOptions.ToString(";") + ";").SingleQuote());
            op.Add("orders", OrderOptions.IsEmpty() ? null : ("[" + OrderOptions.ToString(oo => oo.ToString().SingleQuote(), ",") + "]"));
            op.Add("columns", ColumnOptions.IsEmpty() ? null : (ColumnOptions.ToString(";") + ";").SingleQuote()); 
            op.Add("columnMode", ColumnOptionsMode != ColumnOptionsMode.Add ? ColumnOptionsMode.ToString().SingleQuote() : null);
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
                ElementsPerPage = elementsPerPage ?? FindOptions.DefaultElementsPerPage,
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
                    throw new InvalidOperationException("{0} is not a valid ColumnOptionMode".Formato(ColumnOptionsMode));
            }
        }
    }

    public class FindUniqueOptions
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

        public FindUniqueOptions()
        {
            UniqueType = UniqueType.Single;
        }

        public FindUniqueOptions(object queryName)
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

        public FilterOption(){}

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
            string result = "";
            
            string value = "";

            object v = Common.Convert(Value, Token.Type);
            if (v != null)
            {
                if (v.GetType().IsLite())
                {
                    Lite<IdentifiableEntity> lite = (Lite<IdentifiableEntity>)v;
                    value = lite.Key();
                }
                else
                    value = v.ToString();
            }

            result = "{0},{1},{2}".Formato(ColumnName, Operation.ToString(), EncodeCSV(value));
            //if (Frozen)
            //    result += "&fz{0}=true".Formato(filterIndex);

            return result;
        }

        string EncodeCSV(string p)
        {
            bool hasQuote = p.Contains("\"");
            if (hasQuote || p.Contains(";") || p.Contains(";"))
            {
                if (hasQuote)
                    p = p.Replace("\"", "\"\"");
                return "\"" + p + "\"";
            }
            return p;
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

        public QueryToken Token{ get; set; }
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
    }

    public enum FilterMode
    {
        Visible,
        Hidden,
        AlwaysHidden,
        OnlyResults
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

        public Column ToColumn(QueryDescription qd)
        {
            return new Column(Token, DisplayName.DefaultText(Token.NiceName()));
        }

        public override string ToString()
        {
            return DisplayName.HasText() ? "{0},{1}".Formato(ColumnName, DisplayName) : ColumnName;
        }
    }  
}
