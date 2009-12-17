
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Scheduler;
using Signum.Windows.Scheduler;
using Signum.Windows.Processes;
using Signum.Windows.Operations;
using System.Windows.Media.Imaging;
using System.Reflection;

namespace Signum.Windows.Scheduler
{
    public static class SchedulerClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.Manager.Settings.Add(typeof(ScheduledTaskDN), new EntitySettings(EntityType.Default) { View = e => new ScheduledTask(), Icon = Image("clock.png") });

                OperationClient.Manager.Settings.Add(TaskOperation.ExecutePrivate, new EntityOperationSettings { IsVisible = entity => false });

                Navigator.Manager.Settings.Add(typeof(CustomTaskDN), new EntitySettings(EntityType.ServerOnly) { View = e => new CustomTask(), Icon = Image("customTask.png") });
                Navigator.Manager.Settings.Add(typeof(CustomTaskExecutionDN), new EntitySettings(EntityType.ServerOnly) { View = e => new CustomTaskExecution(), Icon = Image("customTaskExecution.png") });

                OperationClient.Manager.Settings.Add(CustomTaskOperation.Execute, new EntityOperationSettings { Icon = Image("execute.png") });

                Navigator.Manager.Settings.Add(typeof(ScheduleRuleDailyDN), new EntitySettings (EntityType.Default) { View = e => new ScheduleRuleDaily() });
                Navigator.Manager.Settings.Add(typeof(ScheduleRuleWeeklyDN), new EntitySettings (EntityType.Default) { View = e => new ScheduleRuleWeekly() });
                Navigator.Manager.Settings.Add(typeof(ScheduleRuleWeekDaysDN), new EntitySettings (EntityType.Default) { View = e => new ScheduleRuleWeekDays() });
                Navigator.Manager.Settings.Add(typeof(CalendarDN), new EntitySettings (EntityType.Default) { View = e => new Calendar() });
            }
        }

        static BitmapFrame Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(SchedulerClient)));
        }
    }
}
