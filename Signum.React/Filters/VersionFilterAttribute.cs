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
            if (actionContext.Response != null)
                actionContext.Response.Headers.Add("X-App-Version", CurrentVersion);
        }
    }
}