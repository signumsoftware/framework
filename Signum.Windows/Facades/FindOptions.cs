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
    public class FindOptions : MarkupExtension
    {
        public FindOptions()
        {
            this.ShowFilterButton = this.ShowFilters = this.ShowFooter = this.ShowHeader = true;
        }

        public FindOptions(object queryName):this()
        {
            this.QueryName = queryName;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        private SearchButtons buttons = SearchButtons.OkCancel;
        public SearchButtons Buttons
        {
            get { return this.buttons; }
            set { this.buttons = value; }
        }

        private bool? modal;
        public bool Modal
        {
            get { return modal ?? this.Buttons == SearchButtons.OkCancel; }
            set { this.modal = new bool?(value); }
        }

        List<FilterOption> filterOptions = new List<FilterOption>();
        public List<FilterOption> FilterOptions
        {
            get { return filterOptions; }
            set { this.filterOptions = value; }
        }

        public bool AllowMultiple { get; set; }

        public object QueryName { get; set; }

        public OnLoadMode OnLoadMode { get; set; }

        public bool ShowFilters { get; set; }
        public bool ShowFilterButton { get; set; }
        public bool ShowHeader { get; set; }
        public bool ShowFooter { get; set; }

        public EventHandler Closed { get; set; }

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

        public void RefreshRealValue()
        {
            RealValue = Column != null ? Server.Convert(Value, Column.Type) : Value;
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

    public enum SearchButtons
    {
        OkCancel,
        Close
    }

    public enum OnLoadMode
    {
        None,
        Search,
        SearchAndReturnIfOne,
    }

}
