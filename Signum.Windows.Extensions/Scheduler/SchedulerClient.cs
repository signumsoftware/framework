
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
using System.Windows.Media;
using Signum.Utilities;
using Signum.Services;

namespace Signum.Windows.Scheduler
{
    public static class SchedulerClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>()
                {              
                    new EntitySettings<ScheduledTaskEntity> { View = e => new ScheduledTask(), Icon = Image("clock.png") },

                    new EntitySettings<SimpleTaskSymbol> { View = e => new SimpleTask(), Icon = Image("simpleTask.png") },
                    new EntitySettings<ScheduledTaskLogEntity> { View = e => new ScheduledTaskLog(), Icon = Image("scheduledTaskLog.png") },

                    new EntitySettings<ScheduleRuleDailyEntity> { View = e => new ScheduleRuleDaily() },
                    new EntitySettings<ScheduleRuleWeeklyEntity> { View = e => new ScheduleRuleWeekly() },
                    new EntitySettings<ScheduleRuleWeekDaysEntity> { View = e => new ScheduleRuleWeekDays() },
                    new EntitySettings<ScheduleRuleMinutelyEntity> { View = e => new ScheduleRuleMinutely() },
                    new EntitySettings<ScheduleRuleHourlyEntity> { View = e => new ScheduleRuleHourly() },
                    new EntitySettings<HolidayCalendarEntity> { View = e => new HolidayCalendar() },
                });

                Server.SetSymbolIds<SimpleTaskSymbol>();

                var executeGroup = new EntityOperationGroup
                {
                    Background = Brushes.Gold,
                    AutomationName = "execute",
                    Text = () => TaskMessage.Execute.NiceToString() + "...",
                };

                OperationClient.AddSetting(new EntityOperationSettings<ITaskEntity>(TaskOperation.ExecuteSync) { Icon = Image("execute.png"), Group = executeGroup });
                OperationClient.AddSetting(new EntityOperationSettings<ITaskEntity>(TaskOperation.ExecuteAsync) { Icon = Image("execute.png"), Group = executeGroup });
            }
        }

        static BitmapSource Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(SchedulerClient)));
        }
    }
}
