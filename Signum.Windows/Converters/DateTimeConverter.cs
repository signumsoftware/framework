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
        public static readonly DateTimeConverter DateAndTime = new DateTimeConverter(CultureInfo.CurrentCulture.DateTimeFormat.ShortDateTimePattern());
        public static readonly DateTimeConverter Date = new DateTimeConverter("d");

        public bool Strict { get; set; }

        public string Format { get; set; }
        public DateTimeConverter(string format)
        {
            this.Format = format;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? dt = (DateTime?)value;
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
                    return DateTime.ParseExact(str, Format, culture);
                else
                    return DateTime.Parse(str, culture);
            }
            else
                return null;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string str = (string)value;

            DateTime result;
            if (str.HasText())
            {
                if (Strict)
                {
                    if (!DateTime.TryParseExact(str, Format, cultureInfo, DateTimeStyles.None, out result))
                        return new ValidationResult(false, Properties.Resources.InvalidDateFormat);
                }
                else
                {
                     if (!DateTime.TryParse(str, cultureInfo, DateTimeStyles.None, out result))
                         return new ValidationResult(false, Properties.Resources.InvalidDateFormat);
                }
            }
            return new ValidationResult(true, null);
        }
    }
}
