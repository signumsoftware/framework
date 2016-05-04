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
using Signum.Entities.Processes;
using Signum.Engine.Processes;
using System.Threading;
using Signum.Entities.Scheduler;
using Signum.Engine.Scheduler;

namespace Signum.React.Scheduler
{
    public class SchedulerController : ApiController
    {
        [Route("api/scheduler/view"), HttpGet]
        public SchedulerState View()
        {
            var state = SchedulerLogic.GetSchedulerState();

            return state;
        }

        [Route("api/scheduler/start"), HttpPost]
        public void Start()
        {
            SchedulerPermission.ViewSchedulerPanel.AssertAuthorized();

            SchedulerLogic.StartScheduledTasks();

            Thread.Sleep(1000);
        }

        [Route("api/scheduler/stop"), HttpPost]
        public void Stop()
        {
            SchedulerPermission.ViewSchedulerPanel.AssertAuthorized();

            SchedulerLogic.StopScheduledTasks();

            Thread.Sleep(1000);
        }
    }
}