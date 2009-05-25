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
    public class DateTimeConverter : ValidationRule, IValueConverter
    {
        public static readonly DateTimeConverter DateAndTime = new DateTimeConverter(CultureInfo.CurrentCulture.DateTimeFormat.Map(dt => dt.ShortDatePattern + " " + dt.ShortTimePattern));
        public static readonly DateTimeConverter DateOnly = new DateTimeConverter(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);

        string format;
        public DateTimeConverter(string format)
        {
            this.format = format;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? dt = (DateTime?)value;
            if (dt.HasValue)
                return dt.Value.ToString(format, culture);
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = (string)value;
            if (!string.IsNullOrEmpty(str))
                return DateTime.ParseExact(str, format, culture);
            else
                return null;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string str = (string)value;

            DateTime result;
            if (str.HasText() && !DateTime.TryParseExact(str, format, cultureInfo, DateTimeStyles.None, out result))
                return new ValidationResult(false, Properties.Resources.InvalidDateFormat);
            return new ValidationResult(true, null);
        }
    }
}
