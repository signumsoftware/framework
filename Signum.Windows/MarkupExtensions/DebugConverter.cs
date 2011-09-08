using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Markup;
using System.Globalization;

namespace Signum.Windows
{
    public class DebugConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }


        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }

    /// Markup extension to debug databinding
    /// </summary>
    public class DebugBindingExtension : MarkupExtension
    {/// <summary>
        /// Creates a new instance of the Convertor for debugging
        /// </summary>
        /// <param name=”serviceProvider”></param>
        /// <returns>Return a convertor that can be debugged to see the values for the binding</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new DebugConvertor();
        }
    }
}
