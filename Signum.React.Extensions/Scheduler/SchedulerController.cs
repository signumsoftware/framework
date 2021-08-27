using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using System.Threading;
using Signum.Entities.Scheduler;
using Signum.Engine.Scheduler;

namespace Signum.React.Scheduler
{
    public class SchedulerController : ControllerBase
    {
        [HttpGet("api/scheduler/view")]
        public SchedulerState View()
        {
            var state = SchedulerLogic.GetSchedulerState();

            return state;
        }

        [HttpPost("api/scheduler/start")]
        public void Start()
        {
            SchedulerPermission.ViewSchedulerPanel.AssertAuthorized();

            SchedulerLogic.StartScheduledTasks();

            Thread.Sleep(1000);
        }

        [HttpPost("api/scheduler/stop")]
        public void Stop()
        {
            SchedulerPermission.ViewSchedulerPanel.AssertAuthorized();

            SchedulerLogic.StopScheduledTasks();

            Thread.Sleep(1000);
        }
    }
}
