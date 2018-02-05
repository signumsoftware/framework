using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Signum.React.Filters
{
    public class VersionFilterAttribute : ActionFilterAttribute
    {
        public static Assembly CustomAssembly;

        public override void OnActionExecuted(HttpActionExecutedContext actionContext)
        {
            base.OnActionExecuted(actionContext);

            var version = CustomAssembly != null ? CustomAssembly.GetName().Version : Assembly.GetExecutingAssembly().GetName().Version;

            actionContext.Response.Headers.Add("X-App-Version", version.ToString());
        }
    }
}