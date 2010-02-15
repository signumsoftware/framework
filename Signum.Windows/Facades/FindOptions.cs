using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Windows.DynamicQuery;
using System.Windows.Controls;

namespace Signum.Windows
{
    public class CountOptions
    {
        public object QueryName { get; set; }

        public CountOptions() { }
        public CountOptions(object queryName)
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

    public abstract class FindOptionsBase 
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

        public FindOptionsBase()
        {
            this.ShowFilterButton = this.ShowFilters = this.ShowFooter = this.ShowHeader = true;
        }

        public bool SearchOnLoad { get; set; }

        public bool ShowFilters { get; set; }
        public bool ShowFilterButton { get; set; }
        public bool ShowHeader { get; set; }
        public bool ShowFooter { get; set; }

        public string WindowTitle { get; set; }

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

        public ExploreOptions(object queryName, string path, object value)
        {
            this.QueryName = queryName;
            this.FilterOptions.Add(new FilterOption(path, value));
            this.NavigateIfOne = true;
        }

        public EventHandler Closed { get; set; }

        public bool NavigateIfOne { get; set; }

        internal override SearchMode GetSearchMode()
        {
            return SearchMode.Explore;
        }
    }

    public class ExploreExtension : MarkupExtension
    {
        public object QueryName { get; set; }

        public bool NavigateIfOne { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new ExploreOptions(QueryName) { NavigateIfOne = NavigateIfOne };
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

    public class FilterOption : Freezable
    {
        public FilterOption(){}

        public FilterOption(string path, object value)
        {
            this.Path = path;
            this.Operation = FilterOperation.EqualTo;
            this.Value = value; 
        }

        public QueryToken Token { get; set; }
        public string Path { get; set; }
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
            RealValue = Token != null ? Server.Convert(Value, Token.Type) : Value;
            if (ValueChanged != null)
                ValueChanged(this, EventArgs.Empty);
        }

        public Filter ToFilter()
        {
            return new Filter
            {
                Token = Token,
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

        public override string ToString()
        {
            return "{0} {1} {2}".Formato(Path, Operation, Value);
        }
    }

    public class OrderOption : Freezable
    {
        public OrderOption () 
        { 
        }

        public OrderOption (string path)
        {
            this.Path = path;
        }

        public OrderOption(string path, OrderType orderType)
        {
            this.Path = path;
            this.OrderType = orderType;
        }

        protected override Freezable CreateInstanceCore()
        {
            throw new NotImplementedException();
        }

        public string Path { get; set; }
        public QueryToken Token { get; set; }
        public OrderType OrderType { get; set; }
        
        internal ColumnOrderInfo ColumnOrderInfo; 

        public Order ToOrder()
        {
            return new Order(Token, OrderType);
        }
    }

    public class UserColumnOption : Freezable
    {
        public UserColumnOption()
        {
        }

        public UserColumnOption(string path)
        {
            this.Path = path;
        }

        protected override Freezable CreateInstanceCore()
        {
            throw new NotImplementedException();
        }

        public string Path { get; set; }
        public string DisplayName { get; set; }

        public UserColumn UserColumn { get; set; }

        internal GridViewColumn GridViewColumn;
    }  
}
