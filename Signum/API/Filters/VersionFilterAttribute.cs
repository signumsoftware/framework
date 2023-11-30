using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Signum.Utilities.Reflection;

namespace Signum.API.Filters;

public class VersionFilterAttribute : ActionFilterAttribute
{   
    public static Assembly MainAssembly { get; set; } = Assembly.GetEntryAssembly()!;
    public static string? BuildTimeUTC;
    public static string? Version; 

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        if (context.HttpContext.Response != null)
        {
            if (Version == null)
                Version = MainAssembly.GetName().Version!.ToString();

            if (BuildTimeUTC == null)
                BuildTimeUTC = MainAssembly.BuildTimeUTC().ToString("o");

            context.HttpContext.Response.Headers.Append("X-App-Version", new StringValues(Version));
            context.HttpContext.Response.Headers.Append("X-App-BuildTime", new StringValues(BuildTimeUTC));
        }
    }
}
