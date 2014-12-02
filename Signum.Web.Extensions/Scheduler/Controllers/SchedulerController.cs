using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Threading;
using Signum.Services;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Engine.Basics;
using Signum.Engine.Authorization;
using Signum.Web.Operations;
using Signum.Entities.Scheduler;
using Signum.Engine.Scheduler;

namespace Signum.Web.Scheduler
{
    public class SchedulerController : Controller
    {
        [HttpGet]
        public new ActionResult View()
        {
            var state = SchedulerLogic.GetSchedulerState();

            return View(SchedulerClient.ViewPrefix.FormatWith("SchedulerPanel"), state);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Start()
        {
            SchedulerPermission.ViewSchedulerPanel.AssertAuthorized();

            SchedulerLogic.StartScheduledTasks();

            Thread.Sleep(1000);

            return RedirectToAction("View");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Stop()
        {
            SchedulerPermission.ViewSchedulerPanel.AssertAuthorized();

            SchedulerLogic.StopScheduledTasks();

            Thread.Sleep(1000);

            return RedirectToAction("View");
        }
    }
}
