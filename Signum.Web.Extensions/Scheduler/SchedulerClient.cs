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
using Signum.Web.Omnibox;
using Signum.Engine.Authorization;
#endregion

namespace Signum.Web.Scheduler
{
    public static class SchedulerClient
    {
        public static string ViewPrefix = "~/scheduler/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Scheduler/Scripts/Scheduler");

        public static void Start(bool simpleTask)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(SchedulerClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ScheduledTaskLogDN>{ PartialViewName = _ => ViewPrefix.Formato("ScheduledTaskLog") },
                    new EntitySettings<ScheduledTaskDN>{ PartialViewName = _ => ViewPrefix.Formato("ScheduledTask") },
                    
                    new EntitySettings<ScheduleRuleDailyDN> { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleDaily") },
                    new EntitySettings<ScheduleRuleWeeklyDN>{ PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleWeekly") },
                    new EntitySettings<ScheduleRuleWeekDaysDN> { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleWeekDays") },
                    new EntitySettings<ScheduleRuleMinutelyDN> { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleMinutely") },
                    new EntitySettings<ScheduleRuleHourlyDN> { PartialViewName = _ => ViewPrefix.Formato("ScheduleRuleHourly") },

                    new EntitySettings<HolidayCalendarDN> { PartialViewName = _ => ViewPrefix.Formato("HolidayCalendar") },
                    new EmbeddedEntitySettings<HolidayDN> { PartialViewName = _ => ViewPrefix.Formato("Holiday") },
                });

                Navigator.EntitySettings<ScheduleRuleDailyDN>().MappingMain.AsEntityMapping().SetProperty(srd => srd.StartingOn, Mapping.DateHourMinute);
                Navigator.EntitySettings<ScheduleRuleWeeklyDN>().MappingMain.AsEntityMapping().SetProperty(srw => srw.StartingOn, Mapping.DateHourMinute);
                Navigator.EntitySettings<ScheduleRuleWeekDaysDN>().MappingMain.AsEntityMapping().SetProperty(srw => srw.StartingOn, Mapping.DateHourMinute);

                if (simpleTask)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<SimpleTaskSymbol>{ PartialViewName = _ => ViewPrefix.Formato("SimpleTask") },
                    });
                }
                
                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("SchedulerPanel",
                    () => SchedulerPermission.ViewSchedulerPanel.IsAuthorized(),
                    uh => uh.Action((SchedulerController sc) => sc.View())));
            }
        }
    }
}

