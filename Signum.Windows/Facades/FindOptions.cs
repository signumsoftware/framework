using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Windows
{
    public class QueryOptions
    {
        public object QueryName { get; set; }

        public QueryOptions() { }
        public QueryOptions(object queryName)
        {
            this.QueryName = queryName;
        }

        List<FilterOption> filterOptions = new List<FilterOption>();
        public List<FilterOption> FilterOptions
        {
            get { return filterOptions; }
            set { this.filterOptions = value; }
        }
    }

    public abstract class FindOptionsBase : QueryOptions
    {
        public FindOptionsBase()
        {
            this.ShowFilterButton = this.ShowFilters = this.ShowFooter = this.ShowHeader = true;
        }

        public bool SearchOnLoad { get; set; }

        public bool ShowFilters { get; set; }
        public bool ShowFilterButton { get; set; }
        public bool ShowHeader { get; set; }
        public bool ShowFooter { get; set; }

        internal abstract SearchMode GetSearchMode();
    }

    public class FindManyOptions : FindOptionsBase
    {
        public FindManyOptions() { }
        public FindManyOptions(object queryName)
        {
            this.QueryName = queryName;
        }

        internal override SearchMode GetSearchMode()
        {
            return SearchMode.Find;
        }
    }

    public class FindOptions : FindOptionsBase
    {
        public FindOptions() { }
        public FindOptions(object queryName) 
        {
            this.QueryName = queryName;
        }

        public bool ReturnIfOne { get; set; }

        internal override SearchMode GetSearchMode()
        {
            return SearchMode.Find;
        }
    }

    public class ExploreOptions : FindOptionsBase
    {
        public ExploreOptions () { }
        public ExploreOptions(object queryName) 
        {
            this.QueryName = queryName;
        }

        public EventHandler Closed { get; set; }

        public bool NavigateIfOne { get; set; }

        internal override SearchMode GetSearchMode()
        {
            return SearchMode.Explore;
        }
    }

    public class Explore : MarkupExtension
    {
        public object QueryName { get; set; }

        public bool NavigateIfOne { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new ExploreOptions(QueryName) { NavigateIfOne = NavigateIfOne };
        }
    }

    public class FindUniqueOptions : QueryOptions
    {
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

    public class FilterOption : Freezable
    {
        public FilterOption(){}

        public FilterOption(string columnName, object value)
        {
            this.ColumnName = columnName;
            this.Operation = FilterOperation.EqualTo;
            this.Value = value; 
        }

        public Column Column { get; set; }
        public string ColumnName { get; set; }
        public bool Frozen { get; set; }
        public FilterOperation Operation { get; set; }

        public static readonly DependencyProperty ValueProperty =
             DependencyProperty.Register("Value", typeof(object), typeof(FilterOption), new UIPropertyMetadata((d, e) => ((FilterOption)d).RefreshRealValue()));
        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty RealValueProperty =
            DependencyProperty.Register("RealValue", typeof(object), typeof(FilterOption), new UIPropertyMetadata(null));
        public object RealValue
        {
            get { return (object)GetValue(RealValueProperty); }
            set { SetValue(RealValueProperty, value); }
        }

        public event EventHandler ValueChanged;

        public void RefreshRealValue()
        {
            RealValue = Column != null ? Server.Convert(Value, Column.Type) : Value;
            if (ValueChanged != null)
                ValueChanged(this, EventArgs.Empty); 
        }

        public Filter ToFilter()
        {
            return new Filter
            {
                Name = Column.Name,
                Type = Column.Type,
                Operation = Operation,
                Value = RealValue
            };
        }

        protected override Freezable CreateInstanceCore()
        {
            throw new NotImplementedException();
        }

        public static object DefaultValue(Type type)
        {
            if (!type.IsValueType || type.IsNullable())
                return null;

            if (type == typeof(DateTime))
                return DateTime.Now;

            return Activator.CreateInstance(type);
        }
    }
}
