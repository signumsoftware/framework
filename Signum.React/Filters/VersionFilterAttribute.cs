using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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

            SetHeader(actionContext.Response.Headers);
        }

        private void SetHeader(HttpResponseHeaders headers)
        {
            if (!headers.Contains("X-App-Version"))
                headers.Add("X-App-Version", CurrentVersion);
        }

        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            await base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);

            SetHeader(actionExecutedContext.Response.Headers);
        }
    }
}