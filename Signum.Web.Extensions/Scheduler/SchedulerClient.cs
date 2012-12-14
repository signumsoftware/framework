#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
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
                    new EntitySettings<ScheduledTaskDN>(){ PartialViewName = _ => ViewPrefix.Formato("ScheduledTask") },
                    
                    new EntitySettings<ScheduleRuleDailyDN>() { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleDaily") },
                    new EntitySettings<ScheduleRuleWeeklyDN>(){ PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleWeekly") },
                    new EntitySettings<ScheduleRuleWeekDaysDN>() { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleWeekDays") },
                    new EntitySettings<ScheduleRuleMinutelyDN>() { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleMinutely") },
                    new EntitySettings<ScheduleRuleHourlyDN>() { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleHourly") },

                    new EntitySettings<CalendarDN>() { PartialViewName = _ => ViewPrefix.Formato("Calendar") },
                    new EmbeddedEntitySettings<HolidayDN>() { PartialViewName = _ => ViewPrefix.Formato("Holiday") },
                });

                Navigator.EntitySettings<ScheduleRuleDailyDN>().MappingMain.AsEntityMapping().SetProperty(srd => srd.StartingOn, Mapping.DateHourMinute);
                Navigator.EntitySettings<ScheduleRuleWeeklyDN>().MappingMain.AsEntityMapping().SetProperty(srw => srw.StartingOn, Mapping.DateHourMinute);
                Navigator.EntitySettings<ScheduleRuleMinutelyDN>().MappingMain.AsEntityMapping().SetProperty(srw => srw.StartingOn, Mapping.DateHourMinute);
                Navigator.EntitySettings<ScheduleRuleHourlyDN>().MappingMain.AsEntityMapping().SetProperty(srw => srw.StartingOn, Mapping.DateHourMinute);
            }
        }

        
    }
}

