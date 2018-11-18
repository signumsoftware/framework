using System.Reflection;
using Signum.Engine.Scheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Signum.React.Scheduler
{
    public static class SchedulerServer
    {
        public static void Start(IApplicationBuilder app, IApplicationLifetime lifetime)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            lifetime.ApplicationStopping.Register(() =>
            {
                if (SchedulerLogic.Running)
                    SchedulerLogic.StopScheduledTasks();

                SchedulerLogic.StopRunningTasks();
            });
        }
    }
}