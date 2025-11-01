using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Signum.API;

namespace Signum.Scheduler;

public static class SchedulerServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        wsb.WebApplication.Lifetime.ApplicationStopping.Register(() =>
        {
            if (ScheduleTaskRunner.Running)
                ScheduleTaskRunner.StopScheduledTasks();

            ScheduleTaskRunner.StopRunningTasks();
        });
    }
}
