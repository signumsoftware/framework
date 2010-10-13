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

        int? top; //If null, use QuerySettings one
        public int? Top
        {
            get { return top; }
            set { top = value; }
        }

        public FindOptions() { }

        public FindOptions(object queryName)
        {
            this.QueryName = queryName;
        }

        public bool? AllowUserColumns { get; set; }

        public bool SearchOnLoad { get; set; }
        
        public bool? AllowMultiple { get; set; }
        
        FilterMode filterMode = FilterMode.Visible;
        public FilterMode FilterMode
        {
            get { return filterMode; }
            set 
            { 
                this.filterMode = value;
                if (value == FilterMode.OnlyResults)
                    SearchOnLoad = true;
            }
        }

        bool create = true;
        public bool Create
        {
            get { return create; }
            set { create = value; }
        }

        public string Creating { get; set; }

        bool view = true;
        public bool View
        {
            get { return view; }
            set { view = value; }
        }

        public bool Async { get; set; }

        bool entityContextMenu = ContextualItemsHelper.EntityCtxMenuInSearchWindow;
        public bool EntityContextMenu
        {
            get { return entityContextMenu; }
            set { entityContextMenu = value; }
        }

        public override string ToString()
        {
            Navigator.SetTokens(QueryName, FilterOptions);

            string options = new Sequence<string>
            {
                Top.HasValue ? "sfTop=" + Top.Value : null,
                SearchOnLoad ? "sfSearchOnLoad=true" : null,
                !Create ? "sfCreate=false": null, 
                !View ? "sfView=false": null, 
                Async ? "sfAsync=true": null, 
                AllowMultiple.HasValue ? "sfAllowMultiple=" + AllowMultiple.ToString() : null,
                FilterOptions.Select((fi,i)=>fi.ToString(i)),
            }.NotNull().ToString("&");

            if (options.HasText())
                return Navigator.FindRoute(QueryName) + "?" + options;
            else
                return Navigator.FindRoute(QueryName); 
        }

        public void Fill(JsOptionsBuilder op)
        {
            Navigator.SetTokens(this.QueryName, this.FilterOptions); 

            op.Add("queryUrlName", QueryName.TryCC(qn => Navigator.Manager.QuerySettings[qn].UrlName.SingleQuote()));
            op.Add("searchOnLoad", SearchOnLoad == true ? "true" : null);
            op.Add("filterMode", FilterMode != FilterMode.Visible ? FilterMode.ToString().SingleQuote() : null);
            op.Add("create", !Create ? "false" : null);
            op.Add("allowMultiple", AllowMultiple.TrySC(b => b ? "true" : "false"));
            op.Add("filters", filterOptions.Empty() ? null : filterOptions.Select((f, i) => f.ToString(i)).ToString("&").SingleQuote());
            op.Add("allowUserColumns", AllowUserColumns.HasValue ? (AllowUserColumns.Value ? "true" : "false") : null);
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
                Limit = top,
            };
        }

        public List<Column> MergeColumns()
        {
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(QueryName);

            switch (ColumnOptionsMode)
            {
                case ColumnOptionsMode.Add:
                    return qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd)).Concat(ColumnOptions.Select(co => co.ToColumn(qd))).ToList();
                case ColumnOptionsMode.Remove:
                    return qd.Columns.Where(cd => !cd.IsEntity && !ColumnOptions.Any(co => co.ColumnName == cd.Name)).Select(cd => new Column(cd)).ToList();
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
            Filter f = new Filter
            {
                Token = Token,
                Operation = Operation,                
            };

            f.Value = Utils.Convert(Value, f.Token.Type);

            return f;
        }


        public string ToString(int filterIndex)
        {
            string result = "";
            
            string value = "";

            object v = Utils.Convert(Value, Token.Type);
            if (v != null)
            {
                if (typeof(Lite).IsAssignableFrom(v.GetType()))
                {
                    Lite lite = (Lite)v;
                    value = "{0};{1}".Formato(lite.Id.ToString(), lite.RuntimeType.Name);
                }
                else
                    value = v.ToString();
            }

            result = "cn{0}={1}&sel{0}={2}&val{0}={3}".Formato(filterIndex, ColumnName, Operation.ToString(), value);
            if (Frozen)
                result += "&fz{0}=true".Formato(filterIndex);

            return result;
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

        public string ColumnName { get; set; }
        public string DisplayName { get; set; }

        public Column ToColumn(QueryDescription qd)
        {
            var token = QueryUtils.Parse(ColumnName, qd);
            return new Column(token, DisplayName.DefaultText(token.NiceName()));
        }
    }  
}
