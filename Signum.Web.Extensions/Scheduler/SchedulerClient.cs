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
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(SchedulerClient));
                
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ScheduleRuleDailyDN>(EntityType.Default) { PartialViewName = _ => RouteHelper.AreaView("ScheduleRuleDaily", "Scheduler") },
                    new EntitySettings<CustomTaskExecutionDN>(EntityType.Default){ PartialViewName = _ => RouteHelper.AreaView("CustomTaskExecution", "Scheduler") },
                    new EntitySettings<ScheduledTaskDN>(EntityType.Default){ PartialViewName = _ => RouteHelper.AreaView("ScheduledTask", "Scheduler") },
                    new EntitySettings<ScheduleRuleWeeklyDN >(EntityType.Default){ PartialViewName = _ => RouteHelper.AreaView("ScheduleRuleWeekly", "Scheduler") },
                    new EntitySettings<ScheduleRuleWeekDaysDN>(EntityType.Default){ PartialViewName = _ => RouteHelper.AreaView("ScheduleRuleWeekDays", "Scheduler") },
                    new EmbeddedEntitySettings<HolidayDN>(){ PartialViewName = _ => RouteHelper.AreaView("Holiday", "Scheduler") },
                    new EntitySettings<CalendarDN>(EntityType.Default){ PartialViewName = _ => RouteHelper.AreaView("Calendar", "Scheduler") },
                    new EntitySettings<CustomTaskDN>(EntityType.Default){ PartialViewName = _ => RouteHelper.AreaView("CustomTask", "Scheduler") },
                   // new EntitySettings<ScheduleRuleDayDN>(EntityType.Default){ PartialViewName = _ => RouteHelper.AreaView("Calendar", "Scheduler") },
                });
            }
        }
    }
}
