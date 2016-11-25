using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Signum.React.Filters;
using System.Collections.Generic;
using System.Threading;
using System.Web.Http;
using static Signum.Entities.Printing.PrintLogic;

namespace Signum.React.Processes
{
    public class PrintController : ApiController
    {
        [Route("api/printing/stats"), HttpGet]
        public List<PrintStat> Stats()
        {
            return GetReadyToPrintStats();           
        }
    }
}