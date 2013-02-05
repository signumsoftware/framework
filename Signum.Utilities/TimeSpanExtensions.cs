using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Properties;
using System.Globalization;
using System.Linq.Expressions;

namespace Signum.Utilities
{
    public static class TimeSpanExtensions
    {
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
