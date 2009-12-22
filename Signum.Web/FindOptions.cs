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
    public class FindOptions
    {
        public FindOptions() { }

        public object QueryName { get; set; }

        public bool SearchOnLoad { get; set; }
        
        public FindOptions(object queryName)
        {
            this.QueryName = queryName;
        }

        List<FilterOptions> filterOptions = new List<FilterOptions>();
        public List<FilterOptions> FilterOptions
        {
            get { return filterOptions; }
            set { this.filterOptions = value; }
        }

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
            
            if (filterOptions != null && filterOptions.Count > 0)
            {
                for (int i = 0; i < filterOptions.Count; i++)
                    sb.Append(filterOptions[i].ToString(i));
            }
            string result = sb.ToString();
            if (result.HasText())
                return firstCharacter + result.RemoveLeft(1);
            else
                return result;
        }
    }

    public class FilterOptions
    {
        public Column Column { get; set; }
        public string ColumnName { get; set; }
        public bool Frozen { get; set; }
        public FilterOperation Operation { get; set; }
        public object Value { get; set; }

        public Filter ToFilter()
        {
            Filter f = new Filter
            {
                Name = Column.Name,
                Type = Column.Type,
                Operation = Operation,
            };
            if (!typeof(Lite).IsAssignableFrom(Value.GetType()) || Value == null)
                f.Value = Value;
            else
                f.Value = Lite.Create(Reflector.ExtractLite(Column.Type), Database.Retrieve((Lite)Value));
            return f;
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

    public enum FilterMode
    {
        Visible,
        Hidden,
        AlwaysHidden,
    }
}
