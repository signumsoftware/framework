using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Signum.API;

public class SignumHealthResult : IActionResult
{
    private readonly HealthCheckResult _healthCheckResult;

    public SignumHealthResult(HealthCheckResult healthCheckResult)
    {
        _healthCheckResult = healthCheckResult;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var response = context.HttpContext.Response;
        response.ContentType = "application/json";

        // Determine the status code based on the health check result
        response.StatusCode = _healthCheckResult.Status == HealthStatus.Healthy ? 200 : 503;

        var result = new
        {
            status = _healthCheckResult.Status.ToString(),
            description = _healthCheckResult.Description,
            data = _healthCheckResult.Data
        };

        await response.WriteAsync(JsonSerializer.Serialize(result));
    }
}
