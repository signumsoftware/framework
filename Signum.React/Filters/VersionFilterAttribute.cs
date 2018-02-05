using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Signum.React.Filters
{
    public class VersionFilterAttribute : ActionFilterAttribute
    {
        //In Global.asax: VersionFilterAttribute.CurrentVersion = CustomAssembly.GetName().Version.ToString()
        public static string CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(); 
        
        public override void OnActionExecuted(HttpActionExecutedContext actionContext)
        {
            base.OnActionExecuted(actionContext);
            
            actionContext.Response.Headers.Add("X-App-Version", CurrentVersion);
        }

        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            await base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);

            actionExecutedContext.Response.Headers.Add("X-App-Version", CurrentVersion);
        }
    }
}