using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using Signum.Windows.Properties;
using System.Text.RegularExpressions;

namespace Signum.Windows
{
    public class NullableDecimalConverter : ValidationRule, IValueConverter
    {
        public static string NormalizeToDecimal(string format)
        {
            if(Regex.IsMatch(format, @"^D(\d){0,2}$"))
                return "N0"; 

            return format;
        }

        public static readonly NullableDecimalConverter Integer = new NullableDecimalConverter("N0");
        public static readonly NullableDecimalConverter Number = new NullableDecimalConverter("N2");

        public string Format { get; set; } 
        public NullableDecimalConverter(string format)
        {
            this.Format = format; 
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal? v = (decimal?)value;
            return v == null ? "" : v.Value.ToString(Format);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (string)value;
            return s == "" ? null : (decimal?)decimal.Parse(s, CultureInfo.CurrentCulture);
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string s = (string)value;
            decimal v;
            if (string.IsNullOrEmpty(s))
                return new ValidationResult(true, null);
            else if (!decimal.TryParse(s, out v))
                return new ValidationResult(false, Resources.InvalidFormat);
            else
                return new ValidationResult(true, null);
        }
    }
}
