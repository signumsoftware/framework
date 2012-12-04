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
                    new EntitySettings<ScheduledTaskDN>(EntityType.Main){ PartialViewName = _ => ViewPrefix.Formato("ScheduledTask") },
                    
                    new EntitySettings<ScheduleRuleDailyDN>(EntityType.Part) { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleDaily") },
                    new EntitySettings<ScheduleRuleWeeklyDN >(EntityType.Part){ PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleWeekly") },
                    new EntitySettings<ScheduleRuleWeekDaysDN>(EntityType.Part){ PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleWeekDays") },
                    new EntitySettings<ScheduleRuleMinutelyDN>(EntityType.Part) { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleMinutely") },
                    new EntitySettings<ScheduleRuleHourlyDN>(EntityType.Part) { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleHourly") },

                    new EntitySettings<CalendarDN>(EntityType.Shared){ PartialViewName = _ => ViewPrefix.Formato("Calendar") },
                    new EmbeddedEntitySettings<HolidayDN>(){ PartialViewName = _ => ViewPrefix.Formato("Holiday") },
                });

                Navigator.EntitySettings<ScheduleRuleDailyDN>().MappingMain.AsEntityMapping().SetProperty(srd => srd.StartingOn, Mapping.DateHourMinute);
                Navigator.EntitySettings<ScheduleRuleWeeklyDN>().MappingMain.AsEntityMapping().SetProperty(srw => srw.StartingOn, Mapping.DateHourMinute);
                Navigator.EntitySettings<ScheduleRuleMinutelyDN>().MappingMain.AsEntityMapping().SetProperty(srw => srw.StartingOn, Mapping.DateHourMinute);
                Navigator.EntitySettings<ScheduleRuleHourlyDN>().MappingMain.AsEntityMapping().SetProperty(srw => srw.StartingOn, Mapping.DateHourMinute);
            }
        }

        
    }
}

