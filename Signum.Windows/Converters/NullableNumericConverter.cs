using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities;

namespace Signum.Windows
{
    public class NullableNumericConverter : ValidationRule, IValueConverter
    {
        public static string NormalizeToDecimal(string format)
        {
            if(Regex.IsMatch(format, @"^D(\d){0,2}$"))
                return "N0"; 

            return format;
        }

        public static readonly NullableNumericConverter Integer = new NullableNumericConverter("N0");
        public static readonly NullableNumericConverter Number = new NullableNumericConverter("N2");

        public string Format { get; set; } 
        public NullableNumericConverter(string format)
        {
            this.Format = format; 
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? "" : ((IFormattable)value).ToString(Format, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(string.IsNullOrEmpty((string)value))
                return null;

            string s = (string)value;
            if (ReflectionTools.IsPercentage(Format, culture))
            {
                return ReflectionTools.ParsePercentage(s, targetType, culture);
            }

            return ReflectionTools.Parse(s, targetType, culture); 
        }

        public override ValidationResult Validate(object value, CultureInfo culture)
        {
            string s = (string)value;
            if (string.IsNullOrEmpty(s))
                return new ValidationResult(true, null);

            if (ReflectionTools.IsPercentage(Format, culture))
                s = s.Trim(culture.NumberFormat.PercentSymbol.ToCharArray());

            if (!decimal.TryParse(s, NumberStyles.Number, culture, out decimal v))
                return new ValidationResult(false, ValidationMessage.InvalidFormat.NiceToString());
            else
                return new ValidationResult(true, null);
        }
    }
}
