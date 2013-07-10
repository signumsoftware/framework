
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

                Navigator.AddSetting(new EntitySettings<ActionTaskDN> { View = e => new ActionTask(), Icon = Image("actionTask.png") });
                Navigator.AddSetting(new EntitySettings<ScheduledTaskLogDN> { View = e => new ActionTaskLog(), Icon = Image("actionTaskLog.png") });

                OperationClient.AddSetting(new EntityOperationSettings(TaskOperation.ExecuteSync) { Icon = Image("execute.png") });
                OperationClient.AddSetting(new EntityOperationSettings(TaskOperation.ExecuteAsync) { Icon = Image("execute.png") });

                Navigator.AddSetting(new EntitySettings<ScheduleRuleDailyDN> { View = e => new ScheduleRuleDaily() });
                Navigator.AddSetting(new EntitySettings<ScheduleRuleWeeklyDN> { View = e => new ScheduleRuleWeekly() });
                Navigator.AddSetting(new EntitySettings<ScheduleRuleWeekDaysDN> { View = e => new ScheduleRuleWeekDays() });
                Navigator.AddSetting(new EntitySettings<HolidayCalendarDN> { View = e => new HolidayCalendar() });
            }
        }

        static BitmapSource Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(SchedulerClient)));
        }
    }
}
