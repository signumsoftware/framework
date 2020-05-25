using Microsoft.AspNetCore.Mvc.Filters;
using Signum.Utilities.Reflection;
using System;
using System.Reflection;

namespace Signum.React.Filters
{
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

                context.HttpContext.Response.Headers.Add("X-App-Version", Version);
                context.HttpContext.Response.Headers.Add("X-App-BuildTime", BuildTimeUTC);
            }
        }
    }
}
