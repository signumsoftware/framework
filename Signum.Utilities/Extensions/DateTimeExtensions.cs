using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Signum.Utilities
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpander))]
        public static bool IsInInterval(this DateTime date, DateTime minDate, DateTime maxDate)
        {
            return minDate <= date && date < maxDate;
        }

        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpanderNull))]
        public static bool IsInInterval(this DateTime date, DateTime minDate, DateTime? maxDate)
        {
            return minDate <= date && (maxDate == null || date < maxDate);
        }

        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpanderNullNull))]
        public static bool IsInInterval(this DateTime date, DateTime? minDate, DateTime? maxDate)
        {
            return (minDate == null || minDate <= date) &&
                   (maxDate == null || date < maxDate);
        }

        static void AssertDateOnly(DateTime? date)
        {
            if (date == null)
                return;
            DateTime d = date.Value;
            if (d.Hour != 0 || d.Minute != 0 || d.Second != 0 || d.Millisecond != 0)
                throw new InvalidOperationException("The date has some hours, minutes, seconds or milliseconds");
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpander))]
        public static bool IsInDateInterval(this DateTime date, DateTime minDate, DateTime maxDate)
        {
            AssertDateOnly(date);
            AssertDateOnly(minDate);
            AssertDateOnly(maxDate);
            return minDate <= date && date <= maxDate;
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpanderNull))]
        public static bool IsInDateInterval(this DateTime date, DateTime minDate, DateTime? maxDate)
        {
            AssertDateOnly(date);
            AssertDateOnly(minDate);
            AssertDateOnly(maxDate);
            return (minDate == null || minDate <= date) &&
                   (maxDate == null || date < maxDate);
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpanderNullNull))]
        public static bool IsInDateInterval(this DateTime date, DateTime? minDate, DateTime? maxDate)
        {
            AssertDateOnly(date);
            AssertDateOnly(minDate);
            AssertDateOnly(maxDate);
            return (minDate == null || minDate <= date) &&
                   (maxDate == null || date < maxDate);
        }

        class IsInIntervalExpander : IMethodExpander
        {
            static readonly Expression<Func<DateTime, DateTime, DateTime, bool>> func = (date, minDate, maxDate) => minDate <= date && date < maxDate;

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
            }
        }

        class IsInIntervalExpanderNull : IMethodExpander
        {
            Expression<Func<DateTime, DateTime, DateTime?, bool>> func = (date, minDate, maxDate) => minDate <= date && (maxDate == null || date < maxDate);

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
            }
        }

        class IsInIntervalExpanderNullNull : IMethodExpander
        {
            Expression<Func<DateTime, DateTime?, DateTime?, bool>> func = (date, minDate, maxDate) =>
                (minDate == null || minDate <= date) &&
                (maxDate == null || date < maxDate);

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
            }
        }

        public static int YearsTo(this DateTime start, DateTime end)
        {
            int result = end.Year - start.Year;
            if (end.Month < start.Month || (end.Month == start.Month & end.Day < start.Day))
                result--;

            return result;
        }

        public static int MonthsTo(this DateTime start, DateTime end)
        {
            int result = end.Month - start.Month + (end.Year - start.Year) * 12;
            if (end.Day < start.Day)
                result--;

            return result;
        }

        public static double TotalMonths(this DateTime start, DateTime end)
        {
            int wholeMonths = start.MonthsTo(end);

            double result = wholeMonths;

            DateTime limit = start.AddMonths(wholeMonths);
            DateTime limitMonthStart = limit.MonthStart();
            DateTime nextMonthStart = limitMonthStart.AddMonths(1);

            if (nextMonthStart < end)
            {
                result += ((nextMonthStart - limit).TotalDays / NumberOfDaysAfterOneMonth(limitMonthStart));
                result += ((end - nextMonthStart).TotalDays / NumberOfDaysAfterOneMonth(nextMonthStart));
            }
            else
            {
                result += ((end - limit).TotalDays / NumberOfDaysAfterOneMonth(limitMonthStart));
            }

            return result;
        }

        static double NumberOfDaysAfterOneMonth(DateTime monthStart)
        {
            return (monthStart.AddMonths(1) - monthStart).TotalDays;
        }

        public static DateSpan DateSpanTo(this DateTime min, DateTime max)
        {
            return DateSpan.FromToDates(min, max);
        }

        public static DateTime Add(this DateTime date, DateSpan dateSpan)
        {
            return dateSpan.AddTo(date);
        }

        public static DateTime Min(this DateTime a, DateTime b)
        {
            return a < b ? a : b;
        }

        public static DateTime Max(this DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }

        public static DateTime Min(this DateTime a, DateTime? b)
        {
            if (b == null)
                return a;

            return a < b.Value ? a : b.Value;
        }

        public static DateTime Max(this DateTime a, DateTime? b)
        {
            if (b == null)
                return a;

            return a > b.Value ? a : b.Value;
        }

        public static DateTime? Min(this DateTime? a, DateTime? b)
        {
            if (a == null)
                return b;

            if (b == null)
                return a;

            return a.Value < b.Value ? a.Value : b.Value;
        }

        public static DateTime? Max(this DateTime? a, DateTime? b)
        {
            if (a == null)
                return b;

            if (b == null)
                return a;

            return a.Value > b.Value ? a.Value : b.Value;
        }

        /// <param name="precision">Using Milliseconds does nothing, using Days use DateTime.Date</param>
        public static DateTime TrimTo(this DateTime dateTime, DateTimePrecision precision)
        {
            switch (precision)
            {
                case DateTimePrecision.Days: return dateTime.Date;
                case DateTimePrecision.Hours: return TrimToHours(dateTime);
                case DateTimePrecision.Minutes: return TrimToMinutes(dateTime);
                case DateTimePrecision.Seconds: return TrimToSeconds(dateTime);
                case DateTimePrecision.Milliseconds: return dateTime;
            }
            throw new ArgumentException("precission");
        }

        public static DateTime TrimToSeconds(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
        }

        public static DateTime TrimToMinutes(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);
        }

        public static DateTime TrimToHours(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);
        }

        public static DateTimePrecision GetPrecision(this DateTime dateTime)
        {
            if (dateTime.Millisecond != 0)
                return DateTimePrecision.Milliseconds;
            if (dateTime.Second != 0)
                return DateTimePrecision.Seconds;
            if (dateTime.Minute != 0)
                return DateTimePrecision.Minutes;
            if (dateTime.Hour != 0)
                return DateTimePrecision.Hours;

            return DateTimePrecision.Days;
        }

        static char[] allStandardFormats = new char[] {
        'd', 'D', 'f', 'F', 'g', 'G', 'm', 'M', 'o', 'O', 'r', 'R', 's', 't', 'T', 'u', 'U', 'y', 'Y'
        };

        public static string ToCustomFormatString(string f, CultureInfo culture)
        {
            if (f != null && f.Length == 1 && allStandardFormats.IndexOf(f[0]) != -1)
                return culture.DateTimeFormat.GetAllDateTimePatterns(f[0]).FirstEx();

            return f;
        }

        public static string SmartShortDatePattern(this DateTime date)
        {
            DateTime currentdate = DateTime.Today;
            return SmartShortDatePattern(date, currentdate);
        }

        public static string SmartShortDatePattern(this DateTime date, DateTime currentdate)
        {
            int datediff = (date.Date - currentdate).Days;

            if (-7 <= datediff && datediff <= -2)
                return DateTimeMessage.Last0.NiceToString(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek).FirstUpper());

            if (datediff == -1)
                return DateTimeMessage.Yesterday.NiceToString();

            if (datediff == 0)
                return DateTimeMessage.Today.NiceToString();

            if (datediff == 1)
                return DateTimeMessage.Tomorrow.NiceToString();

            if (2 <= datediff && datediff <= 7)
                return DateTimeMessage.This0.NiceToString(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek).FirstUpper());

            if (date.Year == currentdate.Year)
                return date.ToString("d MMM");

            return date.ToString("d MMMM yyyy");
        }

        public static string SmartDatePattern(this DateTime date)
        {
            DateTime currentdate = DateTime.Today;
            return SmartDatePattern(date, currentdate);
        }

        public static string SmartDatePattern(this DateTime date, DateTime currentdate)
        {
            int datediff = (date.Date - currentdate).Days;

            if (-7 <= datediff && datediff <= -2)
                return DateTimeMessage.Last0.NiceToString(CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(date.DayOfWeek).FirstUpper());

            if (datediff == -1)
                return DateTimeMessage.Yesterday.NiceToString();

            if (datediff == 0)
                return DateTimeMessage.Today.NiceToString();

            if (datediff == 1)
                return DateTimeMessage.Tomorrow.NiceToString();

            if (2 <= datediff && datediff <= 7)
                return DateTimeMessage.This0.NiceToString(CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(date.DayOfWeek).FirstUpper());

            if (date.Year == currentdate.Year)
            {
                string pattern = CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;
                pattern = Regex.Replace(pattern, "('[^']*')?yyy?y?('[^']*')?", "");
                string dateString = date.ToString(pattern);
                return dateString.Trim().FirstUpper();
            }
            return date.ToLongDateString();
        }

        public static string ToAgoString(this DateTime dateTime)
        {
            return ToAgoString(dateTime, dateTime.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);
        }

        public static string ToAgoString(this DateTime dateTime, DateTime now)
        {
            TimeSpan ts = now.Subtract(dateTime);
            string resource = null;
            if (ts.TotalMilliseconds < 0)
                resource = DateTimeMessage.In0.NiceToString();
            else
                resource = DateTimeMessage._0Ago.NiceToString();

            int months = Math.Abs(ts.Days) / 30;
            if (months > 0)
                return resource.FormatWith((months == 1 ? DateTimeMessage._0Month.NiceToString() : DateTimeMessage._0Months.NiceToString()).FormatWith(Math.Abs(months))).ToLower();
            if (Math.Abs(ts.Days) > 0)
                return resource.FormatWith((ts.Days == 1 ? DateTimeMessage._0Day.NiceToString() : DateTimeMessage._0Days.NiceToString()).FormatWith(Math.Abs(ts.Days))).ToLower();
            if (Math.Abs(ts.Hours) > 0)
                return resource.FormatWith((ts.Hours == 1 ? DateTimeMessage._0Hour.NiceToString() : DateTimeMessage._0Hours.NiceToString()).FormatWith(Math.Abs(ts.Hours))).ToLower();
            if (Math.Abs(ts.Minutes) > 0)
                return resource.FormatWith((ts.Minutes == 1 ? DateTimeMessage._0Minute.NiceToString() : DateTimeMessage._0Minutes.NiceToString()).FormatWith(Math.Abs(ts.Minutes))).ToLower();

            return resource.FormatWith((ts.Seconds == 1 ? DateTimeMessage._0Second.NiceToString() : DateTimeMessage._0Seconds.NiceToString()).FormatWith(Math.Abs(ts.Seconds))).ToLower();
        }




        public static long JavascriptMilliseconds(this DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("dateTime should be UTC");

            return (long)new TimeSpan(dateTime.Ticks - new DateTime(1970, 1, 1).Ticks).TotalMilliseconds;
        }

        public static DateTime YearStart(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
        }

        public static DateTime MonthStart(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);
        }

        public static DateTime WeekStart(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind).AddDays(-(int)dateTime.DayOfWeek);
        }

        public static DateTime HourStart(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);
        }

        public static DateTime MinuteStart(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);
        }

        public static DateTime SecondStart(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
        }
        
        public static string ToMonthName(this DateTime dateTime)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateTime.Month);
        }

        public static string ToShortMonthName(this DateTime dateTime)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dateTime.Month);
        }

        public static int WeekNumber(this DateTime dateTime)
        {
            var cc = CultureInfo.CurrentCulture;

            return cc.Calendar.GetWeekOfYear(dateTime, cc.DateTimeFormat.CalendarWeekRule, cc.DateTimeFormat.FirstDayOfWeek);
        }

        /// <summary>
        /// Returns the unix time (also known as POSIX time or epoch time) for the give date time.
        /// </summary>
        /// The unix time is defined as the number of seconds, that have elapsed since Thursday, 1 January 1970 00:00:00 (UTC).
        /// <param name="dateTime"></param>
        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Returns the unix time (also known as POSIX time or epoch time) for the give date time in milliseconds.
        /// </summary>
        /// The unix time is defined as the number of milliseconds, that have elapsed since Thursday, 1 January 1970 00:00:00 (UTC).
        /// <param name="dateTime"></param>
        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum DateTimePrecision
    {
        Days,
        Hours,
        Minutes,
        Seconds,
        Milliseconds,
    }

    public struct DateSpan
    {
        public static readonly DateSpan Zero = new DateSpan(0, 0, 0);

        public readonly int Years;
        public readonly int Months;
        public readonly int Days;

        public DateSpan(int years, int months, int days)
        {
            int sign = Math.Sign(years).DefaultToNull() ?? Math.Sign(months).DefaultToNull() ?? Math.Sign(days);

            if (0 < sign && (months < 0 || days < 0) ||
                sign < 0 && (0 < months || 0 < days))
                throw new ArgumentException("All arguments should have the same sign");

            this.Years = years;
            this.Months = months;
            this.Days = days;
        }

        public static DateSpan FromToDates(DateTime min, DateTime max)
        {
            if (min > max) return FromToDates(max, min).Invert();

            int years = max.Year - min.Year;
            int months = max.Month - min.Month;


            if (max.Day < min.Day)
                months -= 1;

            if (months < 0)
            {
                years -= 1;
                months += 12;
            }

            int days = max.Subtract(min.AddYears(years).AddMonths(months)).Days;

            return new DateSpan(years, months, days);
        }

        public DateTime AddTo(DateTime date)
        {
            return date.AddDays(Days).AddMonths(Months).AddYears(Years);
        }

        public DateSpan Invert()
        {
            return new DateSpan(-Years, -Months, -Days);
        }

        public override string ToString()
        {
            string result = ", ".Combine(
                         Years == 0 ? null :
                         Years == 1 ? DateTimeMessage._0Year.NiceToString().FormatWith(Years) :
                                     DateTimeMessage._0Years.NiceToString().FormatWith(Years),
                         Months == 0 ? null :
                         Months == 1 ? DateTimeMessage._0Month.NiceToString().FormatWith(Months) :
                                      DateTimeMessage._0Months.NiceToString().FormatWith(Months),
                         Days == 0 ? null :
                         Days == 1 ? DateTimeMessage._0Day.NiceToString().FormatWith(Days) :
                                    DateTimeMessage._0Days.NiceToString().FormatWith(Days));

            if (string.IsNullOrEmpty(result))
                result = DateTimeMessage._0Day.NiceToString().FormatWith(0);

            return result;

        }
    }

    public enum DateTimeMessage
    {
        [Description("{0} Day")]
        _0Day,
        [Description("{0} Days")]
        _0Days,
        [Description("{0} Hour")]
        _0Hour,
        [Description("{0} Hours")]
        _0Hours,
        [Description("{0} Minute")]
        _0Minute,
        [Description("{0} Minutes")]
        _0Minutes,
        [Description("{0} Week")]
        _0Week,
        [Description("{0} Weeks")]
        _0Weeks,
        [Description("{0} Month")]
        _0Month,
        [Description("{0} Months")]
        _0Months,
        [Description("{0} Second")]
        _0Second,
        [Description("{0} Seconds")]
        _0Seconds,
        [Description("{0} Year")]
        _0Year,
        [Description("{0} Years")]
        _0Years,
        [Description("{0} ago")]
        _0Ago,
        [Description("Last {0}")]
        Last0,
        [Description("This {0}")]
        This0,
        [Description("In {0}")]
        In0,
        Today,
        Tomorrow,
        Yesterday
    }
}
