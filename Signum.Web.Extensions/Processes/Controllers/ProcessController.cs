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
using Signum.Entities.Operations;
using Signum.Engine.Basics;
using Signum.Web.Extensions.Properties;
using Signum.Entities.Processes;
#endregion

namespace Signum.Web.Processes
{
    public class ProcessController : Controller
    {
        [HttpPost]
        public ContentResult GetProgressExecution(int id)
        {
            decimal progress = Database.Query<ProcessExecutionDN>().Where(pe => 
                    pe.Id == id).Select(pe => pe.Progress).SingleEx() ?? 100;

            return Content(Math.Round(progress, 0).ToString());
        }

        [HttpPost]
        public PartialViewResult FinishProcessNormalPage()
        {
            ProcessExecutionDN process = this.ExtractEntity<ProcessExecutionDN>()
                .ThrowIfNullC("Entity was not possible to Extract");

            return Navigator.NormalControl(this, process);
        }

    }
}
