using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Signum.Utilities;

namespace Signum.Entities
{
    public static class TimeZoneManager
    {
        public static TimeZoneMode Mode { get; set; }

        //TimeZoneInfo.Local is read-only. For ASP.Net or other server providers where local time is needed. 
        [ThreadStatic]
        static TimeZoneInfo overrideTimeZone;

        public static TimeZoneInfo OverrideTimeZone
        {
            get { return overrideTimeZone; }
            set { overrideTimeZone = value; }
        }

        public static DateTime Now
        {
            get
            {
                if (Mode == TimeZoneMode.Local)
                    return DateTime.Now;
                else
                    return DateTime.UtcNow;
            }
        }

        public static DateTime ToUserInterface(this DateTime dbDateTime)
        {
            if (Mode == TimeZoneMode.Local)
                return dbDateTime;
            else
                if (OverrideTimeZone == null)
                    return dbDateTime.ToLocalTime();
                else
                    return TimeZoneInfo.ConvertTimeFromUtc(dbDateTime, OverrideTimeZone);
        }

        public static DateTime FromUserInterface(this DateTime uiDateTime)
        {
            if (Mode == TimeZoneMode.Local)
                return uiDateTime;
            else
                if (OverrideTimeZone == null)
                    return uiDateTime.ToUniversalTime();
                else
                    return TimeZoneInfo.ConvertTimeToUtc(uiDateTime, OverrideTimeZone);
        }
    }

    public enum TimeZoneMode
    {   
        Local,
        Utc,
    }
}
