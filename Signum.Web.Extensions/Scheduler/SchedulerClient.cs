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
                    new EntitySettings<ScheduledTaskLogEntity>{ PartialViewName = _ => ViewPrefix.FormatWith("ScheduledTaskLog") },
                    new EntitySettings<ScheduledTaskEntity>{ PartialViewName = _ => ViewPrefix.FormatWith("ScheduledTask") },
                    
                    new EntitySettings<ScheduleRuleDailyEntity> { PartialViewName = _ => ViewPrefix.FormatWith("ScheduleRuleDaily") },
                    new EntitySettings<ScheduleRuleWeeklyEntity>{ PartialViewName = _ => ViewPrefix.FormatWith("ScheduleRuleWeekly") },
                    new EntitySettings<ScheduleRuleWeekDaysEntity> { PartialViewName = _ => ViewPrefix.FormatWith("ScheduleRuleWeekDays") },
                    new EntitySettings<ScheduleRuleMinutelyEntity> { PartialViewName = _ => ViewPrefix.FormatWith("ScheduleRuleMinutely") },
                    new EntitySettings<ScheduleRuleHourlyEntity> { PartialViewName = _ => ViewPrefix.FormatWith("ScheduleRuleHourly") },

                    new EntitySettings<HolidayCalendarEntity> { PartialViewName = _ => ViewPrefix.FormatWith("HolidayCalendar") },
                    new EmbeddedEntitySettings<HolidayEntity> { PartialViewName = _ => ViewPrefix.FormatWith("Holiday") },
                });

                if (simpleTask)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<SimpleTaskSymbol>{ PartialViewName = _ => ViewPrefix.FormatWith("SimpleTask") },
                    });
                }
                
                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("SchedulerPanel",
                    () => SchedulerPermission.ViewSchedulerPanel.IsAuthorized(),
                    uh => uh.Action((SchedulerController sc) => sc.View())));
            }
        }
    }
}

