#region usings
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
using Signum.Entities.Processes;
using Signum.Engine.Processes;
using Signum.Engine.Authorization;
using Signum.Web.Operations;
#endregion

namespace Signum.Web.Processes
{
    public class ProcessController : Controller
    {
        [HttpPost]
        public ContentResult GetProgressExecution(int id)
        {
            decimal progress = Database.Query<ProcessDN>().Where(pe =>
                    pe.Id == id && pe.State != ProcessState.Error).Select(pe => pe.Progress).SingleOrDefaultEx() ?? 100;

            return Content(Math.Round(progress, 0).ToString());
        }

        [HttpGet]
        public new ActionResult View()
        {
            ProcessLogicState state = ProcessRunnerLogic.ExecutionState();

            if (Request.IsAjaxRequest())
                return PartialView(ProcessesClient.ViewPrefix.Formato("ProcessPanelTable"), state);
            else
                return View(ProcessesClient.ViewPrefix.Formato("ProcessPanel"), state);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Start()
        {
            ProcessPermission.ViewProcessPanel.Authorize();

            ProcessRunnerLogic.StartRunningProcesses();

            Thread.Sleep(1000);

            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Stop()
        {
            ProcessPermission.ViewProcessPanel.Authorize();

            ProcessRunnerLogic.Stop();

            Thread.Sleep(1000);

            return null;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ContextualExecute(string operationFullKey, string prefix, string liteKeys)
        {
            var lites = Navigator.ParseLiteKeys<IdentifiableEntity>(liteKeys);

            ProcessDN process = PackageLogic.CreatePackageOperation(lites, OperationsClient.GetOperationKeyAssert(operationFullKey));

            return Navigator.PopupOpen(this, new PopupNavigateOptions(new TypeContext<ProcessDN>(process, prefix)));
        }
    }
}
