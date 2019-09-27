using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Rest;
using Signum.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Signum.React.RestLog
{
    public class RestLogFilter : ActionFilterAttribute
    {
        const string OriginalResponseStreamKey = "ORIGINAL_RESPONSE_STREAM";

        public RestLogFilter(bool allowReplay)
        {
            AllowReplay = allowReplay;
        }

        public bool AllowReplay { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                var request = context.HttpContext.Request;
                context.HttpContext.Items[OriginalResponseStreamKey] = context.HttpContext.Response.Body;
                context.HttpContext.Response.Body = new MemoryStream();

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
                    Controller = context.Controller.GetType().FullName!,
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

                context.HttpContext.Items.Add(typeof(RestLogEntity).FullName, restLog);

            }
            catch (Exception e)
            {
                e.LogException();
            }
        }

        private string GetRequestBody(HttpRequest request)
        {
            // Allows using several time the stream in ASP.Net Core
            request.EnableBuffering();

            string result;
            // Arguments: Stream, Encoding, detect encoding, buffer size
            // AND, the most important: keep stream opened
            request.Body.Position = 0;
            using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                result = reader.ReadToEnd();
            }

            // Rewind, so the core is not lost when it looks the body for the request
            request.Body.Position = 0;

            return result;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if(context.Exception != null)
            {
                var request = (RestLogEntity)context.HttpContext.Items.GetOrThrow(typeof(RestLogEntity).FullName);
                var originalStream = (Stream)context.HttpContext.Items.GetOrThrow(OriginalResponseStreamKey);
                request.EndDate = TimeZoneManager.Now;
                request.Exception = context.Exception.LogException()?.ToLite();

                RestoreOriginalStream(context);

                using (ExecutionMode.Global())
                    request.Save();
            }

            base.OnActionExecuted(context);
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            try
            {
                var request = (RestLogEntity)context.HttpContext.Items.GetOrThrow(typeof(RestLogEntity).FullName);
                request.EndDate = TimeZoneManager.Now;

                Stream memoryStream = RestoreOriginalStream(context);

                if (context.Exception == null)
                {
                    memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                    request.ResponseBody = Encoding.UTF8.GetString(memoryStream.ReadAllBytes());
                }

                if (context.Exception != null)
                {
                    request.Exception = context.Exception.LogException()?.ToLite();
                }

                using (ExecutionMode.Global())
                    request.Save();
            }
            catch (Exception e)
            {
                e.LogException();
            }
        }

        private static Stream RestoreOriginalStream(FilterContext context)
        {
            var originalStream = (Stream)context.HttpContext.Items.GetOrThrow(OriginalResponseStreamKey);
            var memoryStream = context.HttpContext.Response.Body;
            memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
            memoryStream.CopyTo(originalStream);

            context.HttpContext.Response.Body = originalStream;
            return memoryStream;
        }
    }


}
