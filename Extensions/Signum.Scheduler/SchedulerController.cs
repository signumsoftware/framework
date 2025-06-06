using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Signum.API;
using Signum.API.Filters;

namespace Signum.Scheduler;

public class SchedulerController : ControllerBase
{
    [HttpGet("api/scheduler/view")]
    public SchedulerState View()
    {
        var state = ScheduleTaskRunner.GetSchedulerState();

        return state;
    }

    [HttpGet("api/scheduler/healthCheck"), SignumAllowAnonymous, EnableCors(PolicyName = "HealthCheck")]
    public SignumHealthResult HealthCheck()
    {
        var status = ScheduleTaskRunner.GetHealthStatus();
        return new SignumHealthResult(status);
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
