using Signum.Engine.Scheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Signum.React.Scheduler;

public static class SchedulerServer
{
    public static void Start(IApplicationBuilder app, IHostApplicationLifetime lifetime)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        lifetime.ApplicationStopping.Register(() =>
        {
            if (ScheduleTaskRunner.Running)
                ScheduleTaskRunner.StopScheduledTasks();

            ScheduleTaskRunner.StopRunningTasks();
        });
    }
}
