using Signum.Entities.UserAssets;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.React.UserAssets;
using Signum.Entities;
using Signum.React.ApiControllers;
using Signum.Entities.DynamicQuery;
using Signum.React.Maps;
using Signum.React.Facades;
using Signum.Engine.Cache;
using Signum.Entities.Cache;
using Signum.Engine.Authorization;
using Signum.Engine.Maps;
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