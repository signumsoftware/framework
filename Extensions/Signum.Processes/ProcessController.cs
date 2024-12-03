using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Signum.API;
using Signum.API.Controllers;
using Signum.API.Filters;
using Signum.Dashboard;
using System.ComponentModel.DataAnnotations;

namespace Signum.Processes;

[ValidateModelFilter]
public class ProcessController : ControllerBase
{
    [HttpPost("api/processes/constructFromMany/{operationKey}"), ProfilerActionSplitter("operationKey")]
    public EntityPackTS ConstructFromMany(string operationKey, [Required, FromBody]OperationController.MultiOperationRequest request)
    {
        var type = request.Type == null ? null : TypeLogic.GetType(request.Type);

        var op = request.GetOperationSymbol(operationKey, type!);
        var entity = PackageLogic.CreatePackageOperation(request.Lites, op, request.ParseArgs(op));

        return SignumServer.GetEntityPack(entity);
    }

    [HttpGet("api/processes/view")]
    public ProcessLogicState View()
    {
        ProcessLogicState state = ProcessRunner.ExecutionState();

        return state;
    }

    [HttpPost("api/processes/start")]
    public void Start()
    {
        ProcessPermission.ViewProcessPanel.AssertAuthorized();

        ProcessRunner.StartRunningProcesses();

        Thread.Sleep(1000);
    }

    [HttpPost("api/processes/stop")]
    public void Stop()
    {
        ProcessPermission.ViewProcessPanel.AssertAuthorized();

        ProcessRunner.Stop();

        Thread.Sleep(1000);
    }


    [HttpGet("api/processes/healthCheck"), SignumAllowAnonymous, EnableCors(PolicyName = "HealthCheck")]
    public SignumHealthResult HealthCheck()
    {
        var status = ProcessRunner.GetHealthStatus();

        return new SignumHealthResult(status);
    }
}
