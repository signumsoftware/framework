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
    public List<TimeTrackerEntryTS> Times()
    {
        ProfilerPermission.ViewTimeTracker.AssertAuthorized();

        return TimeTracker.IdentifiedElapseds.Select(pair => new TimeTrackerEntryTS
        {
            key = pair.Key,
            count = pair.Value.Count,
            averageTime = pair.Value.Average,
            totalTime = pair.Value.TotalTime,

            lastTime = pair.Value.LastTime,
            lastDate = pair.Value.LastDate,

            maxTime = pair.Value.MaxTime,
            maxDate = pair.Value.MaxDate,


            minTime = pair.Value.MinTime,
            minDate = pair.Value.MinDate,
        }).ToList();

    }

    public class TimeTrackerEntryTS
    {
        public string key;
        public int count;
        public double averageTime;
        public double totalTime;

        public long lastTime;
        public DateTime lastDate;

        public long maxTime;
        public DateTime maxDate;

        public long minTime;
        public DateTime minDate;
    }
}
