using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Scheduler;
using Signum.Windows.Scheduler;
using Signum.Windows.Processes;

namespace Signum.Windows.Scheduler
{
    public static class SchedulerClient
    {
        public static void Start(bool customTask, bool processTask, bool dialy, bool weekly, bool weekdays)
        {
            if(Navigator.Manager.NotDefined<ScheduledTaskDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ScheduledTaskDN), new EntitySettings(false) { View = () => new ScheduledTask() });
            }

            if (processTask)
            {
                ProcessClient.AsserIsLoaded();
            }

            if (customTask && Navigator.Manager.NotDefined<CustomTaskDN>())
            {
                Navigator.Manager.Settings.Add(typeof(CustomTaskDN), new EntitySettings(true) { View = () => new CustomTask() });
            }

            if (dialy && Navigator.Manager.NotDefined<ScheduleRuleDailyDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ScheduleRuleDailyDN), new EntitySettings(false) { View = () => new ScheduleRuleDaily() });
            }

            if (weekly && Navigator.Manager.NotDefined<ScheduleRuleWeeklyDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ScheduleRuleWeeklyDN), new EntitySettings(false) { View = () => new ScheduleRuleWeekly() });
            }

            if (weekdays && Navigator.Manager.NotDefined<ScheduleRuleWeekDaysDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ScheduleRuleWeekDaysDN), new EntitySettings(false) { View = () => new ScheduleRuleWeekDays() });
            }
        }
    }
}
