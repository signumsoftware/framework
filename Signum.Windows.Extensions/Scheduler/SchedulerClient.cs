
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
                    new EntitySettings<ScheduledTaskDN> { View = e => new ScheduledTask(), Icon = Image("clock.png") },

                    new EntitySettings<SimpleTaskSymbol> { View = e => new SimpleTask(), Icon = Image("simpleTask.png") },
                    new EntitySettings<ScheduledTaskLogDN> { View = e => new ScheduledTaskLog(), Icon = Image("scheduledTaskLog.png") },

                    new EntitySettings<ScheduleRuleDailyDN> { View = e => new ScheduleRuleDaily() },
                    new EntitySettings<ScheduleRuleWeeklyDN> { View = e => new ScheduleRuleWeekly() },
                    new EntitySettings<ScheduleRuleWeekDaysDN> { View = e => new ScheduleRuleWeekDays() },
                    new EntitySettings<ScheduleRuleMinutelyDN> { View = e => new ScheduleRuleMinutely() },
                    new EntitySettings<ScheduleRuleHourlyDN> { View = e => new ScheduleRuleHourly() },
                    new EntitySettings<HolidayCalendarDN> { View = e => new HolidayCalendar() },
                });

                Server.SetSymbolIds<SimpleTaskSymbol>();

                var executeGroup = new EntityOperationGroup
                {
                    Background = Brushes.Gold,
                    AutomationName = "execute",
                    Text = () => TaskMessage.Execute.NiceToString() + "...",
                };

                OperationClient.AddSetting(new EntityOperationSettings<ITaskDN>(TaskOperation.ExecuteSync) { Icon = Image("execute.png"), Group = executeGroup });
                OperationClient.AddSetting(new EntityOperationSettings<ITaskDN>(TaskOperation.ExecuteAsync) { Icon = Image("execute.png"), Group = executeGroup });
            }
        }

        static BitmapSource Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(SchedulerClient)));
        }
    }
}
