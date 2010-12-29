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
        public static string ViewPrefix = "scheduler/Views/";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {


                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(SchedulerClient), "~/scheduler/", "Signum.Web.Extensions.Scheduler."));


                RouteTable.Routes.InsertRouteAt0("scheduler/{resourcesFolder}/{*resourceName}",
                  new { controller = "Resources", action = "Index", area = "scheduler" },
                  new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ScheduleRuleDailyDN>(EntityType.Default) { PartialViewName = _ => ViewPrefix + "ScheduleRuleDaily" },
                    new EntitySettings<CustomTaskExecutionDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix + "CustomTaskExecution" },
                    new EntitySettings<ScheduledTaskDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix + "ScheduledTask" },
                    new EntitySettings<ScheduleRuleWeeklyDN >(EntityType.Default){ PartialViewName = _ => ViewPrefix + "ScheduleRuleWeekly" },
                    new EntitySettings<ScheduleRuleWeekDaysDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix + "ScheduleRuleWeekDays" },
                    new EntitySettings<HolidayDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix + "Holiday" },
                    new EntitySettings<CalendarDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix + "Calendar" },
                    new EntitySettings<CustomTaskDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix + "CustomTask" },
                    new EntitySettings<ScheduleRuleDayDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix + "Calendar" },

                  
                });
            }
        }
    }
}
