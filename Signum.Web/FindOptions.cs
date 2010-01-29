using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Engine;

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

        public FindOptions() { }

        public FindOptions(object queryName)
        {
            this.QueryName = queryName;
        }

        public bool SearchOnLoad { get; set; }
        
        public bool? AllowMultiple { get; set; }
        
        FilterMode filterMode = FilterMode.Visible;
        public FilterMode FilterMode
        {
            get { return filterMode; }
            set { this.filterMode = value; }
        }

        public bool? Create { get; set; }

        public bool? Async { get; set; }

        public string ToString(bool writeQueryUrlName, bool writeAllowMultiple, string firstCharacter)
        {
            StringBuilder sb = new StringBuilder();
            if (writeQueryUrlName)
                sb.Append("&sfQueryUrlName=" + Navigator.Manager.QuerySettings[QueryName].UrlName);

            if (SearchOnLoad)
                sb.Append("&sfSearchOnLoad=true");

            if (Create == false)
                sb.Append("&sfCreate=false");

            if (Async == true)
                sb.Append("$sfAsync=true");

            if (writeAllowMultiple && AllowMultiple.HasValue)
                sb.Append("&sfAllowMultiple="+AllowMultiple.ToString());

            if (FilterOptions != null && FilterOptions.Count > 0)
            {
                for (int i = 0; i < FilterOptions.Count; i++)
                    sb.Append(FilterOptions[i].ToString(i));
            }
            string result = sb.ToString();
            if (result.HasText())
                return firstCharacter + result.RemoveLeft(1);
            else
                return result;
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

            throw new ApplicationException(Properties.Resources.ImposibleConvertObject0From1To2.Formato(obj, objType, type));
        }

        public string ToString(int filterIndex)
        {
            string result = "";
            
            string value = "";
            if (Value != null && typeof(Lite).IsAssignableFrom(Value.GetType()))
            {
                Lite lite = (Lite)Value;
                value = "{0};{1}".Formato(lite.Id.ToString(), lite.RuntimeType.Name);
            }
            else
                value = Value.ToString();

            result = "&cn{0}={1}&sel{0}={2}&val{0}={3}".Formato(filterIndex, ColumnName, Operation.ToString(), value);
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
}
