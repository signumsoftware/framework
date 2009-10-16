using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using Signum.Entities.DynamicQuery;

namespace Signum.Windows
{
    public class FindOptions : MarkupExtension
    {
        public FindOptions() { }

        public FindOptions(object queryName)
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

        List<FilterOptions> filterOptions = new List<FilterOptions>();
        public List<FilterOptions> FilterOptions
        {
            get { return filterOptions; }
            set { this.filterOptions = value; }
        }

        public bool AllowMultiple { get; set; }

        public object QueryName { get; set; }

        public OnLoadMode OnLoadMode { get; set; }

        FilterMode filterMode = FilterMode.Visible;
        public FilterMode FilterMode
        {
            get { return filterMode; }
            set { this.filterMode = value; }
        }
    }

    public class FilterOptions : Freezable
    {
        public FilterOptions(){}

        public FilterOptions(string columnName, object value)
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
             DependencyProperty.Register("Value", typeof(object), typeof(FilterOptions), new UIPropertyMetadata((d, e) => ((FilterOptions)d).RefreshRealValue()));
        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty RealValueProperty =
            DependencyProperty.Register("RealValue", typeof(object), typeof(FilterOptions), new UIPropertyMetadata(null));
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
    }

    public enum FilterMode
    {
        Visible,
        VisibleAndReadOnly,
        Hidden,
        AlwaysHidden,
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
