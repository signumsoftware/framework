//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Globalization;
using System.Collections.Generic;
using Signum.Utilities;


namespace Signum.Windows.DateUtils
{
    /// <summary>
    /// WeekNumberConverter uses current FirstDayOfWeek and DateTime to calculate the week numbers
    /// </summary>
    public sealed class WeekNumberConverter : IMultiValueConverter
    {
        /// <summary>
        /// calculate week number with FirstDayOfWeek and DateTime
        /// </summary>
        /// <param name="values">
        ///     values[0] : FirstDayOfWeek
        ///     values[1] : DateTime (this value is used to calculate the MonthCalendar
        /// </param>
        /// <param name="targetType">int</param>
        /// <param name="parameter">0~5, row index of each month</param>
        /// <param name="culture">CultureInfo</param>
        /// <returns>week number, -1 if fails</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (values.Length != 2)
            {
                throw new ArgumentException("Wrong argument number.");
            }

            if (values[0] is DayOfWeek && values[1] is DateTime)
            {
                DayOfWeek firstDayOfWeek = (DayOfWeek)values[0];
                DateTime date = (DateTime)values[1];
                int offset = Int32.Parse((string)parameter, CultureInfo.InvariantCulture);

                DateTime firstVisibleDate = new DateTime(date.Year, date.Month, 1);
                DateTime lastVisibleDate = firstVisibleDate.AddMonths(1).AddSeconds(-1);

                int weekNumber = CalendarDataGenerator.GetWeekNumber(date.Year, date.Month, firstDayOfWeek, culture.DateTimeFormat.CalendarWeekRule, offset, firstVisibleDate, lastVisibleDate);
                if (weekNumber != -1)
                {
                    return weekNumber;
                }
            }

            return -1;
        }

        /// <summary>
        /// Not Supported
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }


    /// <summary>
    /// MonthYearHeaderConverter converts the month&year properties of VisibleMonth to string with CultureInfo
    /// </summary>
    public sealed class MonthYearHeaderConverter : IValueConverter
    {
        /// <summary>
        /// convert the month and year to a proper string
        /// </summary>
        /// <param name="value">DateTime</param>
        /// <param name="targetType">string</param>
        /// <param name="parameter">null</param>
        /// <param name="culture">CultureInfo</param>
        /// <returns>title string</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime)
            {
                if (culture == null)
                {
                    culture = CultureInfo.CurrentCulture;
                }

                if (culture.DateTimeFormat != null)
                {
                    DateTime date = (DateTime)value;
                    return date.ToString(culture.DateTimeFormat.YearMonthPattern, culture.DateTimeFormat);
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// Not Supported
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }


    /// <summary>
    /// DayHeaderConverter converts to FirstDayOfWeek and offset to the correct DayOfWeek value
    /// </summary>
    public sealed class DayHeaderConverter : IValueConverter
    {
        /// <summary>
        /// convert FirstDayOfWeek and offset to the correct DayOfWeek value
        /// </summary>
        /// <param name="value">DayOfWeek</param>
        /// <param name="targetType">DayOfWeek</param>
        /// <param name="parameter">0~6, column index of each month</param>
        /// <param name="culture">CultureInfo</param>
        /// <returns>DayOfWeek value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DayOfWeek)
            {
                int offset = Int32.Parse((string)parameter, CultureInfo.InvariantCulture);
                return (DayOfWeek)(((int)value + offset) % 7);
            }

            return null;
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// AbbreviatedDayNameConverter converts the DayOfWeek value to a culture related abbreviated day name string
    /// </summary>
    public sealed class AbbreviatedDayNameConverter : IValueConverter
    {
        /// <summary>
        /// Convert DayOfWeek value to a culture related abbreviated day name string
        /// </summary>
        /// <param name="value">DayOfWeek</param>
        /// <param name="targetType">string</param>
        /// <param name="parameter">null</param>
        /// <param name="culture"></param>
        /// <returns>abbreviated day name string</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DayOfWeek)
            {
                if (culture == null)
                {
                    culture = CultureInfo.CurrentCulture;
                }

                if (culture.DateTimeFormat != null)
                {
                    return culture.DateTimeFormat.AbbreviatedDayNames[(int)value].FirstUpper();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
