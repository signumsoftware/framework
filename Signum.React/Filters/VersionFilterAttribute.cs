using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace Signum.React.Filters
{
    public class VersionFilterAttribute : ActionFilterAttribute
    {
        //In Global.asax: VersionFilterAttribute.CurrentVersion = CustomAssembly.GetName().Version.ToString()
        public static string CurrentVersion = Assembly.GetEntryAssembly()!.GetName().Version!.ToString()!;

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            if (context.HttpContext.Response != null)
                context.HttpContext.Response.Headers.Add("X-App-Version", CurrentVersion);
        }
    }
}
