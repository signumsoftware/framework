using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using Signum.Windows.Properties;

namespace Signum.Windows
{
    public class NullableDecimalConverter : ValidationRule, IValueConverter
    {
        public static readonly NullableDecimalConverter HighPrecisionNumber = new NullableDecimalConverter() { format = "0.0000", numberStyle = NumberStyles.Number };
        public static readonly NullableDecimalConverter Number = new NullableDecimalConverter() { format = "0.00", numberStyle = NumberStyles.Number };
        public static readonly NullableDecimalConverter Integer = new NullableDecimalConverter() { format = "0", numberStyle = NumberStyles.Integer };
        public static readonly NullableDecimalConverter Currency = new NullableDecimalConverter() { format = "N", numberStyle = NumberStyles.Currency };

        string format;
        NumberStyles numberStyle;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal? v = (decimal?)value;
            return v == null ? "" : v.Value.ToString(format);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (string)value;
            return s == "" ? null : (decimal?)decimal.Parse(s, numberStyle, CultureInfo.CurrentCulture);
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string s = (string)value;
            decimal v;
            if (string.IsNullOrEmpty(s))
                return new ValidationResult(true, null);
            else if (!decimal.TryParse(s, numberStyle, CultureInfo.CurrentCulture, out v))
                return new ValidationResult(false, Resources.InvalidFormat);
            else
                return new ValidationResult(true, null);
        }
    }
}
