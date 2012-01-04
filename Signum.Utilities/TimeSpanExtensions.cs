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
                if (ts.Days == 1) s = s.Add(separator, Properties.Resources._0Day.Formato(ts.Days));
                else s = s.Add(separator, Properties.Resources._0Days.Formato(ts.Days));
                return s;
            }

            if (ts.Hours > 0)
            {
                if (ts.Hours == 1) s = s.Add(separator, Properties.Resources._0Hour.Formato(ts.Hours));
                else s = s.Add(separator, Properties.Resources._0Hours.Formato(ts.Hours));
                return s;
            }

            if (ts.Minutes > 0)
            {
                if (ts.Minutes == 1) s = s.Add(separator, Properties.Resources._0Minute.Formato(ts.Minutes));
                else s = s.Add(separator, Properties.Resources._0Minutes.Formato(ts.Minutes));
                return s;
            }

            if (ts.Seconds > 0)
            {
                if (ts.Seconds == 1) s = s.Add(separator, Properties.Resources._0Second.Formato(ts.Seconds));
                else s = s.Add(separator, Properties.Resources._0Seconds.Formato(ts.Seconds));
                return s;
            }
            return s;
        }
    }
}
