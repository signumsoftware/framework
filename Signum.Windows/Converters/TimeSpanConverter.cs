using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using Signum.Utilities;

namespace Signum.Windows
{
    public class TimeSpanConverter : ValidationRule, IValueConverter
    {
        public static readonly TimeSpanConverter Minutes = new TimeSpanConverter(@"hh\:mm");
        public static readonly TimeSpanConverter Seconds = new TimeSpanConverter(@"hh\:mm\:ss");
        public static readonly TimeSpanConverter Standard = new TimeSpanConverter("c");

        public bool Strict { get; set; }

        public string Format { get; set; }
        public TimeSpanConverter(string format)
        {
            this.Format = format;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan? dt = (TimeSpan?)value;
            if (dt.HasValue)
                return dt.Value.ToString(Format, culture);
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = (string)value;
            if (!string.IsNullOrEmpty(str))
            {
                if (Strict)
                    return TimeSpan.ParseExact(str, Format, culture);
                else
                    return TimeSpan.Parse(str, culture);
            }
            else
                return null;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string str = (string)value;

            TimeSpan result;
            if (str.HasText())
            {
                if (Strict)
                {
                    if (!TimeSpan.TryParseExact(str, Format, cultureInfo, out result))
                        return new ValidationResult(false, "Invalid time format");
                }
                else
                {
                    if (!TimeSpan.TryParse(str, cultureInfo, out result))
                        return new ValidationResult(false, "Invalid time format");
                }
            }
            return new ValidationResult(true, null);
        }
    }
}
