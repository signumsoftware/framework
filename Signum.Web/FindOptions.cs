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

        List<UserColumnOption> userColumnOptions = new List<UserColumnOption>();
        public List<UserColumnOption> UserColumnOptions
        {
            get { return userColumnOptions; }
            set { this.userColumnOptions = value; }
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
            set { this.filterMode = value; }
        }

        bool create = true;
        public bool Create
        {
            get { return create; }
            set { create = value; }
        }

        bool view = true;
        public bool View
        {
            get { return view; }
            set { view = value; }
        }

        public bool Async { get; set; }

        public override string ToString()
        {
            Navigator.SetTokens(QueryName, FilterOptions);

            string options = new Sequence<string>
            {
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
            op.Add("filters", filterOptions.Empty() ? null : filterOptions.Select((f, i) => f.ToString(i)).ToString("").SingleQuote());
            op.Add("allowUserColumns", AllowUserColumns.HasValue ? (AllowUserColumns.Value ? "true" : "false") : null);
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

            f.Value = Convert(Value, f.Token.Type);

            return f;
        }

        public static object Convert(object obj, Type type)
        {
            if (obj == null) return null;

            Type objType = obj.GetType();

            if (type.IsAssignableFrom(objType))
                return obj;

            if (typeof(Lite).IsAssignableFrom(objType) && type.IsAssignableFrom(((Lite)obj).RuntimeType))
            {
                Lite lite = (Lite)obj;
                return lite.UntypedEntityOrNull ?? Database.RetrieveAndForget(lite);
            }

            if (typeof(Lite).IsAssignableFrom(type))
            {
                Type liteType = Reflector.ExtractLite(type);

                if (typeof(Lite).IsAssignableFrom(objType))
                {
                    Lite lite = (Lite)obj;
                    if (liteType.IsAssignableFrom(lite.RuntimeType))
                    {
                        if (lite.UntypedEntityOrNull != null)
                            return Lite.Create(liteType, lite.UntypedEntityOrNull);
                        else
                            return Lite.Create(liteType, lite.Id, lite.RuntimeType, lite.ToStr);
                    }
                }

                else if (liteType.IsAssignableFrom(objType))
                {
                    return Lite.Create(liteType, (IdentifiableEntity)obj);
                }
            }

            throw new InvalidOperationException(Properties.Resources.ImposibleConvertObject0From1To2.Formato(obj, objType, type));
        }

        public string ToString(int filterIndex)
        {
            string result = "";
            
            string value = "";

            object v = Convert(Value, Token.Type);
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
        public QueryToken Token{ get; set; }
        public string ColumnName { get; set; }
        public OrderType Type { get; set; }

        public Order ToOrder()
        {
            return new Order(Token, Type);
        }
    }

    public enum FilterMode
    {
        Visible,
        Hidden,
        AlwaysHidden,
    }

    public class UserColumnOption
    {
        public UserColumnOption()
        {
        }

        public string DisplayName { get; set; }

        public UserColumn UserColumn { get; set; }
    }  
}
