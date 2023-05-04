using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Signum.API.Filters;
using Signum.Processes;
using Signum.Files;

namespace Signum.Printing;

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
