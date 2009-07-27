using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;

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

        private bool? allowMultiple = true;
        public bool? AllowMultiple
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
