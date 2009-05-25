//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;

namespace Signum.Windows.DateUtils
{
    /// <summary>
    /// This class is used to save the cost of Boxing/UnBoxing boolean values
    /// </summary>
    internal static class BooleanBoxes
    {
        internal static object TrueBox = true;
        internal static object FalseBox = false;

        internal static object Box(bool value)
        {
            if (value)
            {
                return TrueBox;
            }
            else
            {
                return FalseBox;
            }
        }
    }

    /// <summary>
    /// Static helper class
    /// </summary>
    internal static class MonthCalendarHelper
    {
        /// <summary>
        /// Compare the year and month of dt1 and dt2
        /// </summary>
        /// <returns>
        /// less than 0    : dt1 &lt; dt2; 
        /// equal 0        : dt1 == dt2; 
        /// greater than 0 : dt1 &gt; dt2
        /// </returns>
        internal static int CompareYearMonth(DateTime dt1, DateTime dt2)
        {
            return SubtractByMonth(dt1, dt2);
        }

        /// <summary>
        /// Compare the year,month,day of dt1 and dt2
        /// </summary>
        /// <returns>
        /// less than 0    : dt1 &lt; dt2; 
        /// equal 0        : dt1 == dt2; 
        /// greater than 0 : dt1 &gt; dt2
        /// </returns>
        internal static int CompareYearMonthDay(DateTime dt1, DateTime dt2)
        {
            return (dt1.Date - dt2.Date).Days;
        }

        /// <summary>
        /// dt1 subtract dt2 to get the month count between them
        /// </summary>
        /// <returns>the months between dt1 and dt2</returns>
        internal static int SubtractByMonth(DateTime dt1, DateTime dt2)
        {
            return (dt1.Year - dt2.Year) * 12 + (dt1.Month - dt2.Month);
        }

        /// <summary>
        /// True if date is between start and end
        /// </summary>
        internal static bool IsWithinRange(DateTime date, DateTime start, DateTime end)
        {
            return CompareYearMonthDay(date, start) >= 0 && CompareYearMonthDay(date, end) <= 0;
        }
    }
}