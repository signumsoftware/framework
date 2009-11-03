using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;

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

        private bool allowMultiple = true;
        public bool AllowMultiple
        {
            get { return allowMultiple; }
            set { allowMultiple = value; }
        }
        
        FilterMode filterMode = FilterMode.Visible;
        public FilterMode FilterMode
        {
            get { return filterMode; }
            set { this.filterMode = value; }
        }

        public bool? Create { get; set; }

        public string ToString(bool writeQueryUrlName, string firstCharacter)
        {
            StringBuilder sb = new StringBuilder();
            if (writeQueryUrlName)
                sb.Append("&sfQueryUrlName=" + Navigator.Manager.QuerySettings[QueryName].UrlName);

            if (SearchOnLoad)
                sb.Append("&sfSearchOnLoad=true");
            sb.Append("&sfAllowMultiple="+AllowMultiple.ToString());
            if (filterOptions != null && filterOptions.Count > 0)
            {
                for (int i = 0; i < filterOptions.Count; i++)
                {
                    FilterOptions fo = filterOptions[i];
                    string value;
                    if (typeof(Lite).IsAssignableFrom(fo.Value.GetType()))
                        value = ((Lite)fo.Value).Id.ToString();
                    else
                        value = fo.Value.ToString();
                    sb.Append("&cn{0}={1}&sel{0}={2}&val{0}={3}".Formato(i, fo.ColumnName, fo.Operation.ToString(), value));
                    if (filterOptions[i].Frozen)
                        sb.Append("&fz{0}=true".Formato(i));
                }
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
            return new Filter
            {
                Name = Column.Name,
                Type = Column.Type,
                Operation = Operation,
                Value = Value 
            };
        }
    }

    public enum FilterMode
    {
        Visible,
        Hidden,
        AlwaysHidden,
    }
}
