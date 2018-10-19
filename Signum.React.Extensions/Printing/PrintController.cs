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
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.Processes
{
    public class PrintController : ApiController
    {
        [HttpGet("api/printing/stats")]
        public List<PrintStat> Stats()
        {
            return PrintingLogic.GetReadyToPrintStats();           
        }

        [HttpPost("api/printing/createProcess")]
        public ProcessEntity Stats([FromBody]FileTypeSymbol fileType)
        {
            return PrintingLogic.CreateProcess(fileType);
        }
    }
}