using System.Linq;
using Signum.Engine;
using Signum.Scheduler;
using Signum.Utilities;

namespace Signum.Scheduler
{
    public static class ScheduledTaskChecker
    {
        /// <summary>
        /// calling by ScheduleTaskActivator_VerifactuTask dinamic event
        /// </summary>
        /// <param name="sts"></param>
        /// <param name="suspend"></param>
        /// <returns></returns>
        public static List<ScheduledTaskEntity>  ActivateOrSuspendAll(this SimpleTaskSymbol sts, bool suspend)
        {
            var toSuspend = Database.Query<ScheduledTaskEntity>()
              .Where(s => s.Task.Is(sts) && s.Suspended != suspend)
               .ToList();

            foreach (var item in toSuspend)
            {
                item.Suspended = suspend;
                item.Execute(ScheduledTaskOperation.Save);
            }

            return toSuspend;   
        }


        public static (bool Queued, bool Running) CheckSimpleTask(this SimpleTaskSymbol sts)
        {
            var scheduledTasks = SchedulerLogic.ScheduledTasksLazy.Value
                .Where(st => st.Task != null && st.Task.Equals(sts))
                .ToList();

            var state = ScheduleTaskRunner.GetSchedulerState();
            var queued = scheduledTasks.Any() &&
                         state.Queue.Any(q => scheduledTasks.Any(st => st.ToLite().Key() == q.ScheduledTask.Key()));

            var running = ScheduleTaskRunner.RunningTasks.Keys
                .Any(log => log.ScheduledTask != null && log.ScheduledTask.Task != null &&
                            log.ScheduledTask.Task.Equals(sts));

            return (queued, running);
        }
    }
}
