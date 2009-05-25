//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Signum.Windows.DateUtils
{
    /// <summary>
    /// Calculate the key date per client's input
    /// The most important date includs:
    ///      - Leading date
    ///      - Trailing date
    /// </summary>
    internal static class CalendarDataGenerator
    {       
        static CalendarDataGenerator()
        {
            _minDate = new DateTime(1753, 1, 1);
            _maxDate = new DateTime(9998, 12, 31);
            _calendar = new GregorianCalendar();
        }

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        internal static DateTime MinDate
        {
            get { return _minDate; }
        }

        internal static DateTime MaxDate
        {
            get { return _maxDate; }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// calculate the trailing date
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="firstDayOfWeek"></param>
        /// <returns></returns>
        internal static DateTime CalculateTrailingDate(DateTime startDate, DateTime endDate, DayOfWeek firstDayOfWeek)
        {
            DateTime trailingDate;

            try
            {
                DateTime tmpStartDate = new DateTime(endDate.Year, endDate.Month, 1);
                int offset = CalculateOffset(startDate, tmpStartDate, firstDayOfWeek);

                trailingDate = tmpStartDate.AddDays(42 - offset - 1); // each month panel contains fixed 42 = 7 * 6 cells.
            }
            catch (ArgumentOutOfRangeException)
            {
                trailingDate = DateTime.MaxValue;
            }

            return trailingDate;
        }

        /// <summary>
        /// calculate the leading date
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="firstDayOfWeek"></param>
        /// <returns></returns>
        internal static DateTime CalculateLeadingDate(DateTime startDate, DayOfWeek firstDayOfWeek)
        {
            DateTime leadingDate;

            try
            {
                leadingDate = startDate.AddDays(-CalculateOffset(startDate, startDate, firstDayOfWeek));
            }
            catch (ArgumentOutOfRangeException) // underflow
            {
                leadingDate = DateTime.MinValue;
            }

            return leadingDate;
        }

        // Calculate the offset of the first day of this month
        // 'Offset' means how many days of last month should be displayed in this month
        internal static int CalculateOffset(DateTime startDate, DateTime currentDate, DayOfWeek firstDayOfWeek)
        {
            int offset;

            // If the day of startDate happens to be the FirstDayOfWeek of the calendar,
            // we need to share one row to display 7 days of last week
            if (currentDate.Year == startDate.Year &&
                currentDate.Month == startDate.Month &&
                currentDate.DayOfWeek == firstDayOfWeek)
            {
                offset = 7;
            }
            else
            {
                offset = (currentDate.DayOfWeek - firstDayOfWeek + 7) % 7;
            }

            return offset;
        }

        /// <summary>
        /// calculate the week number.
        /// If the week number is invisible in the row of this month, return -1
        /// </summary>
        internal static int GetWeekNumber(int currentYear, int currentMonth, DayOfWeek firstDayOfWeek, CalendarWeekRule weekRule, int row, DateTime firstVisibleYearMonth, DateTime lastVisibleYearMonth)
        {
            DateTime startDate = new DateTime(firstVisibleYearMonth.Year, firstVisibleYearMonth.Month, 1);
            DateTime endDate = new DateTime(currentYear, currentMonth, DateTime.DaysInMonth(currentYear, currentMonth));

            DateTime leadingDate = CalculateLeadingDate(startDate, firstDayOfWeek);
            DateTime trailingDate = CalculateTrailingDate(startDate, endDate, firstDayOfWeek);

            DateTime firstDateOfThisMonth;
            DateTime endDateOfThisMonth;

            // If the current month equals to start month,
            // the first date of this month should be the leading date
            if (currentMonth == firstVisibleYearMonth.Month && currentYear == firstVisibleYearMonth.Year)
            {
                firstDateOfThisMonth = leadingDate;
            }
            else
            {
                // Otherwise, count the first date of this month from day 1
                firstDateOfThisMonth = new DateTime(currentYear, currentMonth, 1);
            }

            if (currentMonth == lastVisibleYearMonth.Month && currentYear == lastVisibleYearMonth.Year)
            {
                endDateOfThisMonth = trailingDate;
            }
            else
            {
                endDateOfThisMonth = new DateTime(currentYear, currentMonth, DateTime.DaysInMonth(currentYear, currentMonth));
            }

            // The date which is used to calculate the week number
            DateTime date;

            int offset = (firstDateOfThisMonth.DayOfWeek - firstDayOfWeek + 7) % 7;

            try
            {
                if (row == 0)
                {
                    // To calculate the week number of the first row, we pick up the day of the
                    // last column of the first row to avoid week number inconsistency if this
                    // week cross a year:
                    //  1) Get the date at (0,0): firstDateOfThisMonth.AddDays(-offset)
                    //  2) Plus 6 days to get to date of (0, 6)
                    date = firstDateOfThisMonth.AddDays(6 - offset);
                }
                else
                {
                    // To calculate the date of at (row, 0):
                    //  1) Get the date at (0,0): firstDateOfThisMonth.AddDays(-offset)
                    //  2) Plus the delta from (row, 0) to (0,0): AddDays(row * 7)
                    //
                    date = firstDateOfThisMonth.AddDays(row * 7 - offset);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1;
            }

            if (date > endDateOfThisMonth)
            {
                return -1;
            }


            return _calendar.GetWeekOfYear(date, weekRule, firstDayOfWeek);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        static private Calendar _calendar;
        static readonly private DateTime _minDate;
        static readonly private DateTime _maxDate;

        #endregion
    }
}