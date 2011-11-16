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
        static readonly IVariable<TimeZoneInfo> overrideTimeZone = Statics.SessionVariable<TimeZoneInfo>("timeZone");

        public static TimeZoneInfo OverrideTimeZone
        {
            get { return overrideTimeZone.Value; }
            set { overrideTimeZone.Value = value; }
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
                {
                   var result = TimeZoneInfo.ConvertTimeFromUtc(dbDateTime, OverrideTimeZone);
                   if (dbDateTime.Kind == DateTimeKind.Unspecified)
                       result = new DateTime(dbDateTime.Ticks, DateTimeKind.Local); //Convert asserts TimeZoneInfo to be Local if DateTime.King is Local
                   return result;
                }
        }

        public static DateTime FromUserInterface(this DateTime uiDateTime)
        {
            if (Mode == TimeZoneMode.Local)
                return uiDateTime;
            else
                if (OverrideTimeZone == null)
                    return uiDateTime.ToUniversalTime();
                else
                {
                    if (uiDateTime.Kind == DateTimeKind.Local)
                        uiDateTime = new DateTime(uiDateTime.Ticks, DateTimeKind.Unspecified);
                    return TimeZoneInfo.ConvertTimeToUtc(uiDateTime, OverrideTimeZone);
                }
        }
    }

    public enum TimeZoneMode
    {   
        Local,
        Utc,
    }
}
