using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
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

namespace Signum.React.RestLog
{
    public class RestLogFilter : ActionFilterAttribute
    {
        public RestLogFilter(bool allowReplay)
        {
            AllowReplay = allowReplay;
        }

        public bool AllowReplay { get; set; }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            try
            {
                var request = context.HttpContext.Request;

                var connection = context.HttpContext.Features.Get<IHttpConnectionFeature>();
                
                var queryParams = context.HttpContext.Request.Query
                     .Select(a => new QueryStringValueEmbedded { Key = a.Key, Value = a.Value })
                     .ToMList();

                var restLog = new RestLogEntity
                {
                    AllowReplay = this.AllowReplay,
                    HttpMethod = request.Method.ToString(),
                    Url = request.Path.ToString(),
                    QueryString = queryParams,
                    User = UserHolder.Current?.ToLite(),
                    Controller = context.Controller.GetType().FullName,
                    ControllerName = context.Controller.GetType().Name,
                    Action = ((ControllerActionDescriptor)context.ActionDescriptor).ActionName,
 					MachineName = System.Environment.MachineName,
                    ApplicationName = AppDomain.CurrentDomain.FriendlyName,
                    StartDate = TimeZoneManager.Now,
                    UserHostAddress = connection.RemoteIpAddress.ToString(),
                    UserHostName = request.Host.Value,
                    Referrer = request.Headers["Referrer"].ToString(),
                    RequestBody = GetRequestBody(context.HttpContext.Request) //(string)(actionContext.Request.Properties.ContainsKey(SignumAuthenticationFilterAttribute.SavedRequestKey) ?
                        //actionContext.Request.Properties[SignumAuthenticationFilterAttribute.SavedRequestKey] : null)
                };

                context.RouteData.Values.Add(typeof(RestLogEntity).FullName, restLog);

            }
            catch (Exception e)
            {
                e.LogException();
            }
        }

        private string GetRequestBody(HttpRequest request)
        {
            // Allows using several time the stream in ASP.Net Core
            request.EnableRewind();

            string result;
            // Arguments: Stream, Encoding, detect encoding, buffer size 
            // AND, the most important: keep stream opened
            using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                result = reader.ReadToEnd();
            }

            // Rewind, so the core is not lost when it looks the body for the request
            request.Body.Position = 0;

            return result;
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            try
            {
                var request =
                (RestLogEntity)context.RouteData.Values.GetOrThrow(
                    typeof(RestLogEntity).FullName);
                request.EndDate = TimeZoneManager.Now;

                if (context.Exception == null)
                {
                    request.ResponseBody = Encoding.UTF8.GetString(context.HttpContext.Response.Body.ReadAllBytes());
                }

                if (context.Exception != null)
                {
                    request.Exception = context.Exception.LogException()?.ToLite();
                }

                using (ExecutionMode.Global())
                    request.Save();
            }
            catch(Exception e)
            {
                e.LogException();
            }
        }
    }


}