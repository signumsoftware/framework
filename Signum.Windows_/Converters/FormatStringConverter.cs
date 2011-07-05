using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace Signum.Windows
{
    public class FormatStringConverter : IMultiValueConverter
    {
        private string format;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Format(this.format, values);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public string Format
        {
            get { return this.format; }
            set { this.format = value; }
        }
    }
}
