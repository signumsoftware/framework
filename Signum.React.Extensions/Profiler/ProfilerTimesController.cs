using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Engine.Cache;
using Signum.Engine;
using Signum.Entities.Cache;
using Signum.Utilities.ExpressionTrees;
using System.Threading;
using Signum.React.ApiControllers;
using Signum.Engine.Basics;
using Signum.Entities.Profiler;

namespace Signum.React.Profiler
{
    public class ProfilerTimesController : ApiController
    {
        [Route("api/profilerTimes/clear"), HttpPost]
        public void Clear()
        {
            ProfilerPermission.ViewTimeTracker.AssertAuthorized();

            TimeTracker.IdentifiedElapseds.Clear();
        }

        [Route("api/profilerTimes/times"), HttpGet]
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
}