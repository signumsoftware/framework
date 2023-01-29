using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using System.Threading;
using Signum.Entities.Scheduler;
using Signum.Engine.Scheduler;
using Signum.React.Filters;

namespace Signum.React.Scheduler;

public class SchedulerController : ControllerBase
{
    [HttpGet("api/scheduler/view")]
    public SchedulerState View()
    {
        var state = ScheduleTaskRunner.GetSchedulerState();

        return state;
    }

    [HttpGet("api/scheduler/simpleStatus"), SignumAllowAnonymous]
    public SimpleStatus SimpleStatus()
    {
        return ScheduleTaskRunner.GetSimpleStatus();
    }

    [HttpPost("api/scheduler/start")]
    public void Start()
    {
        SchedulerPermission.ViewSchedulerPanel.AssertAuthorized();

        ScheduleTaskRunner.StartScheduledTasks();

        Thread.Sleep(1000);
    }

    [HttpPost("api/scheduler/stop")]
    public void Stop()
    {
        SchedulerPermission.ViewSchedulerPanel.AssertAuthorized();

        ScheduleTaskRunner.StopScheduledTasks();

        Thread.Sleep(1000);
    }
}
