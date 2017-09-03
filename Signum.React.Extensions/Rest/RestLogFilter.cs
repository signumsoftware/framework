using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Rest;
using Signum.React.Filters;
using Signum.Utilities;
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
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var queryParams =
             actionContext.Request.GetQueryNameValuePairs()
                 .Select(a => new QueryStringValueEmbedded { Key = a.Key, Value = a.Value })
                 .ToMList();


            var request = new RestLogEntity
            {
                Url = actionContext.Request.RequestUri.ToString(),
                QueryString = queryParams,
                User = UserHolder.Current?.ToLite(),
                Controller = actionContext.ControllerContext.Controller.ToString(),
                Action = actionContext.ActionDescriptor.ActionName,
                StartDate = TimeZoneManager.Now,
                RequestBody = (string)(actionContext.Request.Properties.ContainsKey(SignumAuthenticationFilterAttribute.SavedRequestKey) ?
                    actionContext.Request.Properties[SignumAuthenticationFilterAttribute.SavedRequestKey] : null)
            };

            actionContext.ControllerContext.RouteData.Values.Add(typeof(RestLogEntity).FullName, request);
        }


        public override Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext,
            CancellationToken cancellationToken)
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

            request.Save();

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