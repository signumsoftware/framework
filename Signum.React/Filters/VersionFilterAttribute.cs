using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Signum.React.Filters
{
    public class VersionFilterAttribute : ActionFilterAttribute
    {
        //In Global.asax: VersionFilterAttribute.CurrentVersion = CustomAssembly.GetName().Version.ToString()
        public static string CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            if (context.HttpContext.Response != null)
                context.HttpContext.Response.Headers.Add("X-App-Version", CurrentVersion);
        }
    }
}