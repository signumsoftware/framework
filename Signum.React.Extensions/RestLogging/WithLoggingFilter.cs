using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using Signum.Engine;
using Signum.Entities;
using Signum.Entities.RestLogging;

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
            var resultstring = actionExecutedContext.Response.Content.ReadAsStringAsync().Result;

            new RequestEntity
            {
                URL = actionExecutedContext.Request.RequestUri.ToString(),
                Values = queryParams,
                Response = resultstring
            }.Save();
            return base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }
    }
}