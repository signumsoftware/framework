using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Signum.Windows
{
    public class EqualsExtension: MarkupExtension, IValueConverter
    {
        object Other { get; set; }

        public EqualsExtension(object other)
        {
            this.Other = other;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return object.Equals(value, Other); 
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool && (bool)value)
                return Other;

            return DependencyProperty.UnsetValue;
        }
    }

    public class NotEqualsExtension : MarkupExtension, IValueConverter
    {
        object Other { get; set; }

        public NotEqualsExtension(object other)
        {
            this.Other = other;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !object.Equals(value, Other);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

   
}
