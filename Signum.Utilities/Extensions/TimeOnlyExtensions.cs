namespace Signum.Utilities
{
    public static class TimeOnlyExtensions
    {
        public static TimeOnly TrimTo(this TimeOnly time, DateTimePrecision precision)
        {
            switch (precision)
            {
                case DateTimePrecision.Hours: return TrimToHours(time);
                case DateTimePrecision.Minutes: return TrimToMinutes(time);
                case DateTimePrecision.Seconds: return TrimToSeconds(time);
                case DateTimePrecision.Milliseconds: return time;
            }
            throw new ArgumentException("precision");
        }

        public static TimeOnly TrimToSeconds(this TimeOnly time)
        {
            return new TimeOnly(time.Hour, time.Minute, time.Second);
        }

        public static TimeOnly TrimToMinutes(this TimeOnly time)
        {
            return new TimeOnly(time.Hour, time.Minute, 0);
        }

        public static TimeOnly TrimToHours(this TimeOnly time)
        {
            return new TimeOnly(time.Hour, 0, 0);
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
}
