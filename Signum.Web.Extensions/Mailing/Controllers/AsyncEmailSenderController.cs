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
using Signum.Entities.Mailing;
using Signum.Engine.Mailing;
using Signum.Engine.Authorization;
using Signum.Web.Operations;

namespace Signum.Web.Mailing
{
    public class AsyncEmailSenderController : Controller
    {
        [HttpGet]
        public new ActionResult View()
        {
            AsyncEmailSenderState state = AsyncEmailSenderLogic.ExecutionState();

            if (Request.IsAjaxRequest())
                return PartialView(MailingClient.ViewPrefix.FormatWith("AsyncEmailSenderDashboard"), state);
            else
                return View(MailingClient.ViewPrefix.FormatWith("AsyncEmailSenderDashboard"), state);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Start()
        {
            AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel.AssertAuthorized();

            AsyncEmailSenderLogic.StartRunningEmailSenderAsync(0);

            Thread.Sleep(1000);

            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Stop()
        {
            AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel.AssertAuthorized();

            AsyncEmailSenderLogic.Stop();

            Thread.Sleep(1000);

            return null;
        }
    }
}