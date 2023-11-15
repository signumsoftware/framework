
using System.Diagnostics;

namespace Signum.Utilities;

public static class TimeOnlyExtensions
{
    public static TimeOnly TrimTo(this TimeOnly time, DateTimePrecision precision)
    {
        switch (precision)
        {
            case DateTimePrecision.Hours: return TruncHours(time);
            case DateTimePrecision.Minutes: return TruncMinutes(time);
            case DateTimePrecision.Seconds: return TruncSeconds(time);
            case DateTimePrecision.Milliseconds: return time;
        }
        throw new ArgumentException("precision");
    }

    public static TimeOnly TruncSeconds(this TimeOnly time)
    {
        return new TimeOnly(time.Hour, time.Minute, time.Second);
    }

    public static TimeOnly TruncSeconds(this TimeOnly time, int step)
    {
        return new TimeOnly(time.Hour, time.Minute, (time.Second / step) * step);
    }

    public static TimeOnly TruncMilliseconds(this TimeOnly time, int step)
    {
        return new TimeOnly(time.Hour, time.Minute, time.Second, (time.Millisecond / step) * step);
    }

    public static TimeOnly TruncMinutes(this TimeOnly time)
    {
        return new TimeOnly(time.Hour, time.Minute, 0);
    }

    public static TimeOnly TruncMinutes(this TimeOnly time, int step)
    {
        return new TimeOnly(time.Hour, (time.Minute / step) * step, 0);
    }

    public static TimeOnly TruncHours(this TimeOnly time)
    {
        return new TimeOnly(time.Hour, 0, 0);
    }

    public static TimeOnly TruncHours(this TimeOnly time, int step)
    {
        return new TimeOnly((time.Hour / step) * step, 0, 0);
    }



    public static DateTimePrecision? GetPrecision(this TimeOnly timeOnly)
    {
        if (timeOnly.Second != 0)
            return DateTimePrecision.Seconds;
        if (timeOnly.Minute != 0)
            return DateTimePrecision.Minutes;
        if (timeOnly.Hour != 0)
            return DateTimePrecision.Hours;

        return null;
    }
}
