using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Signum.Utilities;

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

    class IsInIntervalExpander : IMethodExpander
    {
        static readonly Expression<Func<DateTime, DateTime, DateTime, bool>> func = (date, minDate, maxDate) => minDate <= date && date < maxDate;

        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
        }
    }

    class IsInIntervalExpanderNull : IMethodExpander
    {
        Expression<Func<DateTime, DateTime, DateTime?, bool>> func = (date, minDate, maxDate) => minDate <= date && (maxDate == null || date < maxDate);

        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
        }
    }

    class IsInIntervalExpanderNullNull : IMethodExpander
    {
        Expression<Func<DateTime, DateTime?, DateTime?, bool>> func = (date, minDate, maxDate) =>
            (minDate == null || minDate <= date) &&
            (maxDate == null || date < maxDate);

        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
        }
    }

    public static int YearsTo(this DateTime start, DateTime end)
    {
        int result = end.Year - start.Year;
        if (end < start.AddYears(result))
            result--;

        return result;
    }

    public static int YearsTo(this DateOnly start, DateOnly end)
    {
        int result = end.Year - start.Year;
        if (end < start.AddYears(result))
            result--;

        return result;
    }

    public static int MonthsTo(this DateTime start, DateTime end)
    {
        int result = end.Month - start.Month + (end.Year - start.Year) * 12;
        if (end < start.AddMonths(result))
            result--;

        return result;
    }

    public static int MonthsTo(this DateOnly start, DateOnly end)
    {
        int result = end.Month - start.Month + (end.Year - start.Year) * 12;
        if (end < start.AddMonths(result))
            result--;

        return result;
    }

    public static int DaysTo(this DateOnly start, DateOnly end)
    {
        return end.DayNumber - start.DayNumber;
    }

    public static int DaysTo(this DateTime start, DateTime end)
    {
        return (end.Date - start.Date).Days;
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

    public static DateTime ToDateTime(this DateOnly date)
    {
        return date.ToDateTime(new TimeOnly());
    }

    public static DateTime? ToDateTime(this DateOnly? date)
    {
        return date == null ? null: date.Value.ToDateTime(new TimeOnly());
    }

    public static DateTime ToDateTime(this DateOnly date, DateTimeKind kind)
    {
        return date.ToDateTime(new TimeOnly(), kind);
    }

    public static DateTime? ToDateTime(this DateOnly? date, DateTimeKind kind)
    {
        return date == null ? null : date.Value.ToDateTime(new TimeOnly(), kind);
    }

    public static TimeSpan? ToTimeSpan(this TimeOnly? time)
    {
        return time == null ? null : time.Value.ToTimeSpan();
    }

    public static TimeOnly ToTimeOnly(this TimeSpan time)
    {
        return TimeOnly.FromTimeSpan(time);
    }

    public static TimeOnly? ToTimeOnly(this TimeSpan? time)
    {
        return time == null ? null : TimeOnly.FromTimeSpan(time.Value);
    }

    public static DateOnly ToDateOnly(this DateTime dateTime)
    {
        return DateOnly.FromDateTime(dateTime);
    }

    public static DateOnly? ToDateOnly(this DateTime? dateTime)
    {
        return dateTime == null ? null : DateOnly.FromDateTime(dateTime.Value);
    }


    /// <param name="precision">Using Milliseconds does nothing, using Days use DateTime.Date</param>
    public static DateTime TruncTo(this DateTime dateTime, DateTimePrecision precision)
    {
        return precision switch
        {
            DateTimePrecision.Days => dateTime.Date,
            DateTimePrecision.Hours => TruncHours(dateTime),
            DateTimePrecision.Minutes => TruncMinutes(dateTime),
            DateTimePrecision.Seconds => TruncSeconds(dateTime),
            DateTimePrecision.Milliseconds => dateTime,
            _ => throw new UnexpectedValueException(precision),
        };
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

    public static string? ToCustomFormatString(string? f, CultureInfo culture)
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

    public static string ToIsoString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public static string ToIsoString(this DateOnly dateOnly)
    {
        return dateOnly.ToString("yyyy-MM-dd");
    }

    public static string ToAgoString(this DateTime dateTime)
    {
        return ToAgoString(dateTime, dateTime.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);
    }

    public static string ToAgoString(this DateTime dateTime, DateTime now)
    {
        TimeSpan ts = now.Subtract(dateTime);
        string msg = ts.TotalMilliseconds < 0 ? DateTimeMessage.In0.NiceToString() : DateTimeMessage._0Ago.NiceToString();
        int months = Math.Abs(ts.Days) / 30;
        if (months > 0)
            return msg.FormatWith((months == 1 ? DateTimeMessage._0Month.NiceToString() : DateTimeMessage._0Months.NiceToString()).FormatWith(Math.Abs(months))).ToLower();
        if (Math.Abs(ts.Days) > 0)
            return msg.FormatWith((ts.Days == 1 ? DateTimeMessage._0Day.NiceToString() : DateTimeMessage._0Days.NiceToString()).FormatWith(Math.Abs(ts.Days))).ToLower();
        if (Math.Abs(ts.Hours) > 0)
            return msg.FormatWith((ts.Hours == 1 ? DateTimeMessage._0Hour.NiceToString() : DateTimeMessage._0Hours.NiceToString()).FormatWith(Math.Abs(ts.Hours))).ToLower();
        if (Math.Abs(ts.Minutes) > 0)
            return msg.FormatWith((ts.Minutes == 1 ? DateTimeMessage._0Minute.NiceToString() : DateTimeMessage._0Minutes.NiceToString()).FormatWith(Math.Abs(ts.Minutes))).ToLower();

        return msg.FormatWith((ts.Seconds == 1 ? DateTimeMessage._0Second.NiceToString() : DateTimeMessage._0Seconds.NiceToString()).FormatWith(Math.Abs(ts.Seconds))).ToLower();
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

    public static DateOnly YearStart(this DateOnly date)
    {
        return new DateOnly(date.Year, 1, 1);
    }

    public static DateTime QuarterStart(this DateTime dateTime)
    {
        var quarterMonthStart = (((dateTime.Month - 1) / 4) * 4) + 1;

        return new DateTime(dateTime.Year, quarterMonthStart, 1, 0, 0, 0, dateTime.Kind);
    }

    public static DateOnly QuarterStart(this DateOnly date)
    {
        var quarterMonthStart = (((date.Month - 1) / 4) * 4) + 1;

        return new DateOnly(date.Year, quarterMonthStart, 1);
    }

    public static int Quarter(this DateTime dateTime)
    {
        return ((dateTime.Month - 1) / 3) + 1;
    }

    public static int Quarter(this DateTimeOffset dateTime)
    {
        return ((dateTime.Month - 1) / 3) + 1;
    }

    public static int Quarter(this DateOnly date)
    {
        return ((date.Month - 1) / 3) + 1;
    }

    public static DateTime MonthStart(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);
    }

    public static DateOnly MonthStart(this DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, 1);
    }

    public static DateTime WeekStart(this DateTime dateTime) => dateTime.WeekStart(CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
    public static DateTime WeekStart(this DateTime dateTime, DayOfWeek startOfWeek)
    {
        var date = dateTime.Date;
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.AddDays(-diff);
    }

    public static DateOnly WeekStart(this DateOnly date) => date.WeekStart(CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
    public static DateOnly WeekStart(this DateOnly date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.AddDays(-diff);
    }

    public static DateTime TruncHours(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);
    }

    public static DateTime TruncHours(this DateTime dateTime, int step)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, (dateTime.Hour / step) * step, 0, 0, dateTime.Kind);
    }

    public static DateTime TruncMinutes(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);
    }

    public static DateTime TruncMinutes(this DateTime dateTime, int step)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, (dateTime.Minute / step) * step, 0, dateTime.Kind);
    }

    public static DateTime TruncSeconds(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
    }

    public static DateTime TruncSeconds(this DateTime dateTime, int step)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, (dateTime.Second / step) * step, dateTime.Kind);
    }

    public static DateTime TruncMilliseconds(this DateTime dateTime, int step)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, (dateTime.Millisecond / step) * step, dateTime.Kind);
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

    public static int WeekNumber(this DateOnly date)
    {
        var cc = CultureInfo.CurrentCulture;

        return cc.Calendar.GetWeekOfYear(date.ToDateTime(new TimeOnly()), cc.DateTimeFormat.CalendarWeekRule, cc.DateTimeFormat.FirstDayOfWeek);
    }

    public static int WeekNumber(this DateTimeOffset date)
    {
        var cc = CultureInfo.CurrentCulture;

        return cc.Calendar.GetWeekOfYear(date.Date, cc.DateTimeFormat.CalendarWeekRule, cc.DateTimeFormat.FirstDayOfWeek);
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
