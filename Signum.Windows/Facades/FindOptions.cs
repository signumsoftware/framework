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
using System.Windows.Data;

namespace Signum.Windows
{
    public class QueryCountOptions
    {
        public object QueryName { get; set; }

        public QueryCountOptions() { }
        public QueryCountOptions(object queryName)
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

    public class QueryOptions
    {
        public static Pagination DefaultPagination = new Pagination.Paginate(50, 1);


        public object QueryName { get; set; }
        public bool GroupResults { get; set; }

        public List<FilterOption> FilterOptions { get; set;  } = new List<FilterOption>();
        public List<OrderOption> OrderOptions { get; set; } = new List<OrderOption>();
        public List<ColumnOption> ColumnOptions { get; set; } = new List<ColumnOption>();

        ColumnOptionsMode columnOptionsMode = ColumnOptionsMode.Add;
        public ColumnOptionsMode ColumnOptionsMode
        {
            get { return columnOptionsMode; }
            set { this.columnOptionsMode = value; }
        }
 
        Pagination pagination;
        public Pagination Pagination
        {
            get { return pagination; }
            set { this.pagination = value; }
        }
    }

    public abstract class FindOptionsBase : QueryOptions
    {
        public bool SearchOnLoad { get; set; }

        public bool ShowFilters { get; set; }
        public bool ShowFilterButton { get; set; }
        public bool ShowHeader { get; set; }
        public bool ShowFooter { get; set; }

        public string WindowTitle { get; set; }
        public string EntityTypeTitle { get; set; }
        public string QueryNameTitle { get; set; }

        public abstract SearchMode GetSearchMode();

        public Action<SearchControl> InitializeSearchControl;

        public FindOptionsBase()
        {
            this.ShowFilterButton = this.ShowFilters = this.ShowFooter = this.ShowHeader = true;
        }
    }

    public class FindManyOptions : FindOptionsBase
    {
        public FindManyOptions() { }
        public FindManyOptions(object queryName)
        {
            this.QueryName = queryName;
        }

        public override SearchMode GetSearchMode()
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

        public override SearchMode GetSearchMode()
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

        public ExploreOptions(object queryName, string columnName, object value)
        {
            this.QueryName = queryName;
            this.FilterOptions.Add(new FilterOption(columnName, value));
        }

        public EventHandler Closed { get; set; }

        public bool NavigateIfOne { get; set; }

        public override SearchMode GetSearchMode()
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

    public class FilterOption : Freezable
    {
        public FilterOption(){}

        public FilterOption(string columnName, Func<object> value):
            this(columnName, (object)value)
        {

        }

        public FilterOption(string columnName, object value)
        {
            this.ColumnName = columnName;
            this.Operation = FilterOperation.EqualTo;
            this.Value = value; 
        }

        public QueryToken Token { get; set; }
        public string ColumnName { get; set; }
        public bool Frozen { get; set; }
        public FilterOperation Operation { get; set; }

        public static readonly DependencyProperty ValueProperty =
             DependencyProperty.Register("Value", typeof(object), typeof(FilterOption), new UIPropertyMetadata((d, e) => ((FilterOption)d).ValueChanged(e)));
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

        public event DependencyPropertyChangedEventHandler BindingValueChanged;
        private void ValueChanged(DependencyPropertyChangedEventArgs args)
        {
            RefreshRealValue();

            if (BindingValueChanged != null && BindingOperations.GetBindingExpression(this, ValueProperty) != null)
                BindingValueChanged(this, args);
        }

        public void RefreshRealValue()
        {
            if (Token == null || Value is Func<object>)
                return;

            if (Operation.IsList())
            {
                RealValue = Value;
                return;
            }
            var newRealValue = Server.Convert(Value, Token.Type);

            if (!object.Equals(newRealValue, RealValue))
            {
                RealValue = newRealValue;
            }
        }

        public Filter ToFilter()
        {
            return new FilterCondition(Token, Operation, RealValue);
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
            return "{0} {1} {2}".FormatWith(ColumnName, Operation, Value);
        }

        public FilterOption CloneIfNecessary()
        {
            if (CheckAccess())
                return this;

            return new FilterOption
            {
                 Operation = Operation,
                 Token = Token,
                 ColumnName = ColumnName,
                 Value = Dispatcher.Return(()=>Value)
            };
        }

        public static void SetFilterTokens(IEnumerable<FilterOption> filters, QueryDescription qd, bool canAggregate = false)
        {
            var options = SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0);
            foreach (var f in filters)
            {
                if (f.Token == null && f.ColumnName.HasText())
                    f.Token = QueryUtils.Parse(f.ColumnName, qd, options);

                f.RefreshRealValue();
            }
        }
    }

    public class OrderOption : Freezable
    {
        public OrderOption () 
        { 
        }

        public OrderOption (string columnName)
        {
            this.ColumnName = columnName;
        }

        public OrderOption(string columnName, OrderType orderType)
        {
            this.ColumnName = columnName;
            this.OrderType = orderType;
        }

        protected override Freezable CreateInstanceCore()
        {
            throw new NotImplementedException();
        }

        public string ColumnName { get; set; }
        public QueryToken Token { get; set; }
        public OrderType OrderType { get; set; }

        internal SortGridViewColumnHeader Header; 

        public Order ToOrder()
        {
            return new Order(Token, OrderType);
        }

        public OrderOption CloneIfNecessary()
        {
            if (CheckAccess())
                return this;

            return new OrderOption
            {
                Token = Token,
                ColumnName = ColumnName,
                OrderType = OrderType
            };
        }

        public static void SetOrderTokens(IEnumerable<OrderOption> orders, QueryDescription qd, bool canAggregate = false)
        {
            var options = SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0);
            foreach (var o in orders)
            {
                o.Token = QueryUtils.Parse(o.ColumnName, qd, options);
            }
        }
    }

    public class ColumnOption : Freezable
    {
        public ColumnOption()
        {
        }

        public ColumnOption(string columnName)
        {
            this.ColumnName = columnName;
        }

        protected override Freezable CreateInstanceCore()
        {
            throw new NotImplementedException();
        }

        //For temporaly XAML only
        public string ColumnName { get; set; }
        public QueryToken Token { get; set; }
        public string DisplayName { get; set; }

        internal Column ToColumn()
        {
            return new Column(Token, DisplayName.DefaultText(Token.NiceName()));
        }

        public ColumnOption CloneIfNecessary()
        {
            if (CheckAccess())
                return this;

            return new ColumnOption
            {
                DisplayName = DisplayName,
                Token = Token,
                ColumnName = ColumnName,
            };
        }

        public static void SetColumnTokens(IEnumerable<ColumnOption> columns, QueryDescription qd, bool canAggregate = false)
        {
            var options = SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0);
            foreach (var c in columns)
            {
                c.Token = QueryUtils.Parse(c.ColumnName, qd, options);
            }
        }

        public static List<Column> MergeColumns(IEnumerable<ColumnOption> columns, ColumnOptionsMode mode, QueryDescription qd)
        {
            switch (mode)
            {
                case ColumnOptionsMode.Add:
                    return qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd, qd.QueryName)).Concat(
                        columns.Select(co => co.ToColumn())).ToList();
                case ColumnOptionsMode.Remove:
                    return qd.Columns.Where(cd => !cd.IsEntity && !columns.Any(co => co.ColumnName == cd.Name)).Select(cd => new Column(cd, qd.QueryName)).ToList();
                case ColumnOptionsMode.Replace:
                    return columns.Select(co => co.ToColumn()).ToList();
                default:
                    throw new InvalidOperationException("{0} is not a valid ColumnOptionMode".FormatWith(mode));
            }
        }    
    }
}
