
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Scheduler;
using Signum.Windows.Scheduler;
using Signum.Windows.Processes;
using Signum.Windows.Operations;
using System.Windows.Media.Imaging;

namespace Signum.Windows.Scheduler
{
    public static class SchedulerClient
    {
        public static void Start(bool customTask, bool processTask, bool dialy, bool weekly, bool weekDays)
        {
            if(Navigator.Manager.NotDefined<ScheduledTaskDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ScheduledTaskDN), new EntitySettings(EntityType.Default) { View = () => new ScheduledTask(), Icon = Image("clock.png") });

                OperationClient.Manager.Settings.Add(TaskOperation.ExecutePrivate, new EntityOperationSettings { IsVisible = entity => false });
            }

            if (processTask)
            {
                ProcessClient.AsserIsLoaded();
            }

            if (customTask && Navigator.Manager.NotDefined<CustomTaskDN>())
            {
                Navigator.Manager.Settings.Add(typeof(CustomTaskDN), new EntitySettings(EntityType.ServerOnly) { View = () => new CustomTask(), Icon = Image("customTask.png") });
                Navigator.Manager.Settings.Add(typeof(CustomTaskExecutionDN), new EntitySettings(EntityType.ServerOnly) { View = () => new CustomTaskExecution(), Icon = Image("customTaskExecution.png") });

                OperationClient.Manager.Settings.Add(CustomTaskOperation.Execute, new EntityOperationSettings { Icon = Image("execute.png") });
            }

            if (dialy && Navigator.Manager.NotDefined<ScheduleRuleDailyDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ScheduleRuleDailyDN), new EntitySettings { View = () => new ScheduleRuleDaily() });
            }

            if (weekly && Navigator.Manager.NotDefined<ScheduleRuleWeeklyDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ScheduleRuleWeeklyDN), new EntitySettings { View = () => new ScheduleRuleWeekly() });
            }

            if (weekDays && Navigator.Manager.NotDefined<ScheduleRuleWeekDaysDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ScheduleRuleWeekDaysDN), new EntitySettings { View = () => new ScheduleRuleWeekDays() });
                Navigator.Manager.Settings.Add(typeof(CalendarDN), new EntitySettings { View = () => new Calendar() });
            }
        }

        static BitmapFrame Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(SchedulerClient)));
        }
    }
}
