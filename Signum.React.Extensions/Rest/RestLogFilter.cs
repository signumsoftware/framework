using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Rest;
using Signum.React.Filters;
using Signum.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Signum.React.RestLog
{
    public class RestLogFilter : ActionFilterAttribute
    {
        public RestLogFilter(bool allowReplay)
        {
            AllowReplay = allowReplay;
        }

        public bool AllowReplay { get; set; }

        


        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                var queryParams =
                 actionContext.Request.GetQueryNameValuePairs()
                     .Select(a => new QueryStringValueEmbedded { Key = a.Key, Value = a.Value })
                     .ToMList();

                var request = new RestLogEntity
                {
                    AllowReplay = this.AllowReplay,
                    HttpMethod = actionContext.Request.Method.ToString(),
                    Url = actionContext.Request.RequestUri.ToString(),
                    QueryString = queryParams,
                    User = UserHolder.Current?.ToLite(),
                    Controller = actionContext.ControllerContext.Controller.ToString(),
                    ControllerName = actionContext.ControllerContext.Controller.ToString().AfterLast('.'),
                    Action = actionContext.ActionDescriptor.ActionName,
                    StartDate = TimeZoneManager.Now,
                    UserHostAddress = SignumExceptionFilterAttribute.GetClientIp(actionContext.Request),
                    UserHostName = SignumExceptionFilterAttribute.GetClientName(actionContext.Request),
                    Referrer = actionContext.Request.Headers.Referrer?.ToString(),
                    RequestBody = (string)(actionContext.Request.Properties.ContainsKey(SignumAuthenticationFilterAttribute.SavedRequestKey) ?
                        actionContext.Request.Properties[SignumAuthenticationFilterAttribute.SavedRequestKey] : null)
                };

                actionContext.ControllerContext.RouteData.Values.Add(typeof(RestLogEntity).FullName, request);

            }
            catch (Exception e)
            {
                e.LogException();
            }
        }


        public override Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            try
            {
                var request =
                (RestLogEntity)actionExecutedContext.ActionContext.ControllerContext.RouteData.Values.GetOrThrow(
                    typeof(RestLogEntity).FullName);
                request.EndDate = TimeZoneManager.Now;

                if (actionExecutedContext.Exception == null)
                {
                    request.ResponseBody = actionExecutedContext.Response.Content?.ReadAsStringAsync()?.Result;
                }

                if (actionExecutedContext.Exception != null)
                {
                    request.Exception = actionExecutedContext.Exception.LogException()?.ToLite();
                }

                using (ExecutionMode.Global())
                    request.Save();
            }
            catch(Exception e)
            {
                e.LogException();
            }

            return base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }


        private string GetRequestBody(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                var httpContextBase = (HttpContextWrapper)request.Properties["MS_HttpContext"];

                using (var stream = new MemoryStream())
                {
                    var s = httpContextBase.Request.GetBufferedInputStream();
                    s.CopyTo(stream);
                    string requestBody = Encoding.UTF8.GetString(stream.ToArray());
                    return requestBody;
                }
            }

            return request.Content.ReadAsStringAsync().Result;
        }
    }


}