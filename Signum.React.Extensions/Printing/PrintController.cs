using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Printing;
using Signum.Engine.Processes;
using Signum.Entities.Files;
using Signum.Entities.Processes;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Signum.React.Filters;
using System.Collections.Generic;
using System.Threading;
using System.Web.Http;

namespace Signum.React.Processes
{
    public class PrintController : ApiController
    {
        [Route("api/printing/stats"), HttpGet]
        public List<PrintStat> Stats()
        {
            return PrintingLogic.GetReadyToPrintStats();           
        }

        [Route("api/printing/createProcess"), HttpPost]
        public ProcessEntity Stats(FileTypeSymbol fileType)
        {
            return PrintingLogic.CreateProcess(fileType);
        }
    }
}