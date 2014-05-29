using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;

namespace Signum.Web.Calendar
{
    public static class CalendarClient
    {
        public static string ViewPrefix = "~/Calendar/Views/{0}.cshtml";
        public static JsModule Modules = new JsModule(@"Extensions/Signum.Web.Extensions/Calendar/Scripts/calendar");

        public const string CssInactiveDayDiv = "sf-cal-day-inactive";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(CalendarClient));
            }
        }
    }
}