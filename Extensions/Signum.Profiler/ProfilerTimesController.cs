using Microsoft.AspNetCore.Mvc;

namespace Signum.Profiler;

public class ProfilerTimesController : ControllerBase
{
    [HttpPost("api/profilerTimes/clear")]
    public void Clear()
    {
        ProfilerPermission.ViewTimeTracker.AssertAuthorized();

        TimeTracker.IdentifiedElapseds.Clear();
    }

    [HttpGet("api/profilerTimes/times")]
    public List<TimeTrackerEntry> Times()
    {
        ProfilerPermission.ViewTimeTracker.AssertAuthorized();

        return TimeTracker.IdentifiedElapseds.Values.ToList();

    }
}
