//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Data;


namespace Signum.Windows.DateUtils
{
    /// <summary>
    /// Convert between DateTime and string, it's the default Converter for DatePicker.DateValueConverter
    /// </summary>
    public sealed class DateTimeValueConverter : IValueConverter
    {
        /// <summary>
        /// Convert DateTime to a formatted string(ShortDatePattern)
        /// </summary>
        /// <param name="value">DateTime</param>
        /// <param name="targetType">string</param>
        /// <param name="parameter">null</param>
        /// <param name="culture"></param>
        /// <returns>formatted or empty string</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }
            DateTimeFormatInfo dateTimeFormat = culture.DateTimeFormat;
            if (dateTimeFormat == null)
            {
                throw new ArgumentNullException("culture.DateTimeFormat");
            }

            if (value != null && value is DateTime)
            {
                DateTime date = (DateTime)value;
                return date.ToString(dateTimeFormat.ShortDatePattern, dateTimeFormat);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Convert input string to DateTime
        /// </summary>
        /// <param name="value">string</param>
        /// <param name="targetType">DateTime</param>
        /// <param name="parameter">null</param>
        /// <param name="culture"></param>
        /// <returns>DateTime or null</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            if (value != null && value is string)
            {
                return DateTime.Parse((string)value, culture.DateTimeFormat);
            }
            else
            {
                return null;
            }
        }
    }
}