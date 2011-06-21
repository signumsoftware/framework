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
                
                Navigator.EntitySettings<ScheduleRuleDailyDN>().MappingAdmin.AsEntityMapping().SetProperty(srd => srd.StartingOn, MappingDate);

                Navigator.EntitySettings<ScheduleRuleWeeklyDN>().MappingAdmin.AsEntityMapping().SetProperty(srw => srw.StartingOn, MappingDate);
            }
        }

        public static DateTime MappingDate(MappingContext<DateTime> ctx)
        {
            if (ctx.Parent.Empty())
                return ctx.None();

            DateTime dateStart; 
            int hours; 
            int mins;
            if (ctx.Parent.Parse("Date", out dateStart) & ctx.Parent.Parse("Hour", out hours) & ctx.Parent.Parse("Minute", out mins))
                return dateStart.AddHours(hours).AddMinutes(mins).FromUserInterface();

            return ctx.None();
        }
    }
}
