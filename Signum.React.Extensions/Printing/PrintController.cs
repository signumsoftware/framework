using Signum.Engine.Printing;
using Signum.Entities.Files;
using Signum.Entities.Processes;
using Signum.React.Filters;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Signum.React.Processes
{
    [ValidateModelFilter]
    public class PrintController : ControllerBase
    {
        [HttpGet("api/printing/stats")]
        public List<PrintStat> Stats()
        {
            return PrintingLogic.GetReadyToPrintStats();
        }

        [HttpPost("api/printing/createProcess")]
        public ProcessEntity? Stats([Required, FromBody]FileTypeSymbol fileType)
        {
            return PrintingLogic.CreateProcess(fileType);
        }
    }
}
