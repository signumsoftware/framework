#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Entities.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using Signum.Entities.Basics;
using System.Reflection;

using System.Web.Routing;

using Signum.Entities.Scheduler;
using Signum.Entities.Files;
#endregion

namespace Signum.Web.Extensions.Scheduler
{
    public static class SchedulerClient
    {
        public static string ViewPrefix = "~/scheduler/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(SchedulerClient));
                
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ScheduleRuleDailyDN>(EntityType.Default) { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleDaily") },
                    new EntitySettings<CustomTaskExecutionDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix.Formato("CustomTaskExecution") },
                    new EntitySettings<ScheduledTaskDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix.Formato("ScheduledTask") },
                    new EntitySettings<ScheduleRuleWeeklyDN >(EntityType.Default){ PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleWeekly") },
                    new EntitySettings<ScheduleRuleWeekDaysDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleWeekDays") },
                    new EmbeddedEntitySettings<HolidayDN>(){ PartialViewName = _ => ViewPrefix.Formato("Holiday") },
                    new EntitySettings<CalendarDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix.Formato("Calendar") },
                    new EntitySettings<CustomTaskDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix.Formato("CustomTask") },
                   // new EntitySettings<ScheduleRuleDayDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix.Formato("Calendar") },
                });
                
                Navigator.EntitySettings<ScheduleRuleDailyDN>().MappingAdmin.SetProperty(srd => srd.StartingOn, MappingDate);

                Navigator.EntitySettings<ScheduleRuleWeeklyDN>().MappingAdmin.SetProperty(srw => srw.StartingOn, MappingDate);
            }
        }

        public static DateTime MappingDate(MappingContext<DateTime> ctx)
        {
            string date;
            if (!ctx.Parent.Inputs.TryGetValue("Date", out date))
                return ctx.None();

            DateTime dateStart;
            if (!DateTime.TryParse(date, out dateStart))
                return ctx.ParentNone("Date", "Invalid date");

            string hour;
            if (!ctx.Parent.Inputs.TryGetValue("Hour", out hour))
                return ctx.None();

            int hours;
            if (!int.TryParse(hour, out hours) || hours < 0 || hours > 23)
                return ctx.ParentNone("Hour", "Invalid hour");

            string min;
            if (!ctx.Parent.Inputs.TryGetValue("Minute", out min))
                return ctx.None();

            int mins;
            if (!int.TryParse(min, out mins) || mins < 0 || mins > 59)
                return ctx.ParentNone("Min", "Invalid min");

            return dateStart.AddHours(hours).AddMinutes(mins).FromUserInterface();
        }
    }
}
