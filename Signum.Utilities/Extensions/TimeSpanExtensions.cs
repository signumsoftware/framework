
using System.Diagnostics;

namespace Signum.Utilities;


public static class TimeSpanExtensions
{

    public static TimeSpan TrimTo(this TimeSpan time, DateTimePrecision precision)
    {
        switch (precision)
        {
            case DateTimePrecision.Days: return time.TrimToDays();
            case DateTimePrecision.Hours: return TruncHours(time);
            case DateTimePrecision.Minutes: return TruncMinutes(time);
            case DateTimePrecision.Seconds: return TruncSeconds(time);
            case DateTimePrecision.Milliseconds: return time;
        }
        throw new ArgumentException("precision");
    }

    public static TimeSpan TruncSeconds(this TimeSpan time)
    {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }

    public static TimeSpan TruncSeconds(this TimeSpan time, int step)
    {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, (time.Seconds / step) * step);
    }

    public static TimeSpan TruncMinutes(this TimeSpan time)
    {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, 0);
    }

    public static TimeSpan TruncMinutes(this TimeSpan time, int step)
    {
        return new TimeSpan(time.Days, time.Hours, (time.Minutes / step) * step, 0);
    }

    public static TimeSpan TruncHours(this TimeSpan time)
    {
        return new TimeSpan(time.Days, time.Hours, 0, 0);
    }

    public static TimeSpan TruncHours(this TimeSpan time, int step)
    {
        return new TimeSpan(time.Days, (time.Hours / step) * step, 0, 0);
    }

    public static TimeSpan TrimToDays(this TimeSpan time)
    {
        return new TimeSpan(time.Days, 0, 0, 0);
    }

    public static DateTimePrecision? GetPrecision(this TimeSpan timeSpan)
    {
        if (timeSpan.Milliseconds != 0)
            return DateTimePrecision.Milliseconds;
        if (timeSpan.Seconds != 0)
            return DateTimePrecision.Seconds;
        if (timeSpan.Minutes != 0)
            return DateTimePrecision.Minutes;
        if (timeSpan.Hours != 0)
            return DateTimePrecision.Hours;
        if (timeSpan.Days != 0)
            return DateTimePrecision.Days;

        return null;
    }

    public static string NiceToString(this TimeSpan timeSpan)
    {
        return timeSpan.NiceToString(DateTimePrecision.Milliseconds);
    }

    public static string NiceToString(this TimeSpan timeSpan, DateTimePrecision precision)
    {
        StringBuilder sb = new StringBuilder();
        bool any = false;
        if (timeSpan.Days != 0/* && precision >= DateTimePrecision.Days*/)
        {
            sb.AppendFormat("{0}d", timeSpan.Days);
            any = true;
        }

        if ((any || timeSpan.Hours != 0) && precision >= DateTimePrecision.Hours)
        {
            if (any)
                sb.Append(" ");

            sb.AppendFormat("{0,2}h", timeSpan.Hours);
            any = true;
        }

        if ((any || timeSpan.Minutes != 0) && precision >= DateTimePrecision.Minutes)
        {
            if (any)
                sb.Append(" ");

            sb.AppendFormat("{0,2}m", timeSpan.Minutes);
            any = true;
        }

        if ((any || timeSpan.Seconds != 0) && precision >= DateTimePrecision.Seconds)
        {
            if (any)
                sb.Append(" ");

            sb.AppendFormat("{0,2}s", timeSpan.Seconds);
            any = true;
        }

        if ((any || timeSpan.Milliseconds != 0) && precision >= DateTimePrecision.Milliseconds)
        {
            if (any)
                sb.Append(" ");

            sb.AppendFormat("{0,3}ms", timeSpan.Milliseconds);
        }

        return sb.ToString();
    }

    public static string ToShortString(this TimeSpan ts)
    {
        string s = string.Empty;
        string separator = ", ";

        if (ts.Days > 0)
        {
            if (ts.Days == 1) s = s.Add(separator, DateTimeMessage._0Day.NiceToString().FormatWith(ts.Days));
            else s = s.Add(separator, DateTimeMessage._0Days.NiceToString().FormatWith(ts.Days));
            return s;
        }

        if (ts.Hours > 0)
        {
            if (ts.Hours == 1) s = s.Add(separator, DateTimeMessage._0Hour.NiceToString().FormatWith(ts.Hours));
            else s = s.Add(separator, DateTimeMessage._0Hours.NiceToString().FormatWith(ts.Hours));
            return s;
        }

        if (ts.Minutes > 0)
        {
            if (ts.Minutes == 1) s = s.Add(separator, DateTimeMessage._0Minute.NiceToString().FormatWith(ts.Minutes));
            else s = s.Add(separator, DateTimeMessage._0Minutes.NiceToString().FormatWith(ts.Minutes));
            return s;
        }

        if (ts.Seconds > 0)
        {
            if (ts.Seconds == 1) s = s.Add(separator, DateTimeMessage._0Second.NiceToString().FormatWith(ts.Seconds));
            else s = s.Add(separator, DateTimeMessage._0Seconds.NiceToString().FormatWith(ts.Seconds));
            return s;
        }
        return s;
    }
}
