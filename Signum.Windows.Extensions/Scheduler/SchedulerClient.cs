
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
                Navigator.AddSetting(new EntitySettings<ScheduledTaskDN> { View = e => new ScheduledTask(), Icon = Image("clock.png") });

                Navigator.AddSetting(new EntitySettings<CustomTaskDN> { View = e => new CustomTask(), Icon = Image("customTask.png") });
                Navigator.AddSetting(new EntitySettings<CustomTaskExecutionDN> { View = e => new CustomTaskExecution(), Icon = Image("customTaskExecution.png") });

                OperationClient.AddSetting(new EntityOperationSettings(CustomTaskOperation.Execute){ Icon = Image("execute.png") });

                Navigator.AddSetting(new EntitySettings<ScheduleRuleDailyDN> { View = e => new ScheduleRuleDaily() });
                Navigator.AddSetting(new EntitySettings<ScheduleRuleWeeklyDN> { View = e => new ScheduleRuleWeekly() });
                Navigator.AddSetting(new EntitySettings<ScheduleRuleWeekDaysDN> { View = e => new ScheduleRuleWeekDays() });
                Navigator.AddSetting(new EntitySettings<CalendarDN> { View = e => new Calendar() });
            }
        }

        static BitmapFrame Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(SchedulerClient)));
        }
    }
}
