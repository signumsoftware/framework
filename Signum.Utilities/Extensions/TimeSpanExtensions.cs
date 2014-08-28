using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Linq.Expressions;

namespace Signum.Utilities
{
    public static class TimeSpanExtensions
    {

        public static TimeSpan TrimTo(this TimeSpan time, DateTimePrecision precision)
        {
            switch (precision)
            {
                case DateTimePrecision.Days: return time.TrimToDays();
                case DateTimePrecision.Hours: return TrimToHours(time);
                case DateTimePrecision.Minutes: return TrimToMinutes(time);
                case DateTimePrecision.Seconds: return TrimToSeconds(time);
                case DateTimePrecision.Milliseconds: return time;
            }
            throw new ArgumentException("precission");
        }

        public static TimeSpan TrimToSeconds(this TimeSpan time)
        {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }

        public static TimeSpan TrimToMinutes(this TimeSpan time)
        {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, 0);
        }

        public static TimeSpan TrimToHours(this TimeSpan time)
        {
            return new TimeSpan(time.Days, time.Hours, 0, 0);
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

        public static string NiceToString(this TimeSpan timeSpan, DateTimePrecision precission)
        {
            StringBuilder sb = new StringBuilder();
            bool any = false;
            if (timeSpan.Days != 0/* && precission >= DateTimePrecision.Days*/)
            {
                sb.AppendFormat("{0}d", timeSpan.Days);
                any = true;
            }

            if ((any || timeSpan.Hours != 0) && precission >= DateTimePrecision.Hours)
            {
                if (any)
                    sb.Append(" ");

                sb.AppendFormat("{0,2}h", timeSpan.Hours);
                any = true;
            }

            if ((any || timeSpan.Minutes != 0) && precission >= DateTimePrecision.Minutes)
            {
                if (any)
                    sb.Append(" ");

                sb.AppendFormat("{0,2}m", timeSpan.Minutes);
                any = true;
            }

            if ((any || timeSpan.Seconds != 0) && precission >= DateTimePrecision.Seconds)
            {
                if (any)
                    sb.Append(" ");

                sb.AppendFormat("{0,2}s", timeSpan.Seconds);
                any = true;
            }

            if ((any || timeSpan.Milliseconds != 0) && precission >= DateTimePrecision.Milliseconds)
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
                if (ts.Days == 1) s = s.Add(separator, DateTimeMessage._0Day.NiceToString().Formato(ts.Days));
                else s = s.Add(separator, DateTimeMessage._0Days.NiceToString().Formato(ts.Days));
                return s;
            }

            if (ts.Hours > 0)
            {
                if (ts.Hours == 1) s = s.Add(separator, DateTimeMessage._0Hour.NiceToString().Formato(ts.Hours));
                else s = s.Add(separator, DateTimeMessage._0Hours.NiceToString().Formato(ts.Hours));
                return s;
            }

            if (ts.Minutes > 0)
            {
                if (ts.Minutes == 1) s = s.Add(separator, DateTimeMessage._0Minute.NiceToString().Formato(ts.Minutes));
                else s = s.Add(separator, DateTimeMessage._0Minutes.NiceToString().Formato(ts.Minutes));
                return s;
            }

            if (ts.Seconds > 0)
            {
                if (ts.Seconds == 1) s = s.Add(separator, DateTimeMessage._0Second.NiceToString().Formato(ts.Seconds));
                else s = s.Add(separator, DateTimeMessage._0Seconds.NiceToString().Formato(ts.Seconds));
                return s;
            }
            return s;
        }
    }
}
