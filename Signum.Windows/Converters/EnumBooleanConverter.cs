using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Globalization;
using Signum.Utilities;
using System.ComponentModel;
using Signum.Entities;

namespace Signum.Windows
{
    public class EnumBooleanConverter : IValueConverter
    {
        public static readonly EnumBooleanConverter Instance = new EnumBooleanConverter();

        private EnumBooleanConverter() { }

        public object Convert(object value/*enum*/, Type targetType, object parameter/*enum*/, CultureInfo culture)
        {
            if (value == null || value.GetType().UnNullify() != parameter.GetType().UnNullify())
                return DependencyProperty.UnsetValue;

            return parameter.Equals(value); /*bool*/
        }

        public object ConvertBack(object value/*bool*/, Type targetType, object parameter/*enum*/, CultureInfo culture)
        {
            if (parameter == null || parameter.GetType() != targetType || value.Equals(false))
                return DependencyProperty.UnsetValue;

            return parameter;
        }
    }

    public static class EnumWindowsExtensions
    {
        public static IEnumerable<Enum> PreAndNull(this IEnumerable<Enum> collection)
        {
            return collection.PreAnd(VoidEnumMessage.Instance);
        }

        public static IEnumerable<Enum> PreAndNull(this IEnumerable<Enum> collection, bool isNullable)
        {
            if (isNullable)
                return collection.PreAnd(VoidEnumMessage.Instance);
            return collection;
        }
    }
}
