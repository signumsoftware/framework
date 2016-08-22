using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.RestLogging;
using Signum.Entities.UserAssets;

namespace Signum.React.Logging
{
    public class WithLoggingFilter : ActionFilterAttribute
    {


        public override Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext,
            CancellationToken cancellationToken)
        {
            var queryParams =
                actionExecutedContext.Request.GetQueryNameValuePairs()
                    .Select(a => new RequestValueEntity { Key = a.Key, Value = a.Value })
                    .ToMList();

            var resultstring = "";
            if (actionExecutedContext.Exception==null)
            {
                resultstring = actionExecutedContext.Response.Content.ReadAsStringAsync().Result;
            }
            

         

            var request = new RestRequestEntity
            {
                URL = actionExecutedContext.Request.RequestUri.ToString(),
                QueryString = queryParams,
                User = UserHolder.Current.ToLite(),
                Controller = actionExecutedContext.ActionContext.ControllerContext.Controller.ToString(),
                Action = actionExecutedContext.ActionContext.ActionDescriptor.ActionName,

                Response = resultstring,

            }.Save();

            if (actionExecutedContext.Exception != null)
            {
                request.Exception = actionExecutedContext.Exception.LogException(e => e.ActionName = )
            }


            return base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }
    }
}