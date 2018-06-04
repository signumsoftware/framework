using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Web;

namespace Signum.React.Filters
{
    public class SignumExceptionFilterAttribute : IExceptionFilter
    {
        static Func<ExceptionContext, bool> IncludeErrorDetails = ctx => true;

        public static readonly List<Type> IgnoreExceptions = new List<Type> { typeof(OperationCanceledException) };

        public void OnException(ExceptionContext context)
        {
            if (!IgnoreExceptions.Contains(context.Exception.GetType()))
            {
                var statusCode = GetStatus(context.Exception.GetType());


                var req = context.HttpContext.Request;

                var connFeature = context.HttpContext.Features.Get<IHttpConnectionFeature>();
                
                var exLog = context.Exception.LogException(e =>
                {
                    e.ActionName = (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName;
                    e.ControllerName = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName;
                    e.UserAgent = req.Headers["User-Agent"].FirstOrDefault();
                    e.RequestUrl = req.GetDisplayUrl();
                    e.UrlReferer = req.Headers["Referer"].ToString();
                    e.UserHostAddress = connFeature.RemoteIpAddress.ToString();
                    e.UserHostName = Dns.GetHostEntry(connFeature.RemoteIpAddress).HostName;
                    e.User = UserHolder.Current?.ToLite();
                    e.QueryString = req.QueryString.ToString();
                    e.Form = ReadAllBody(context.HttpContext);
                    e.Session = null;
                });

                var error = new HttpError(context.Exception);
                context.HttpContext.Response.StatusCode = (int)statusCode;
                context.Result = new JsonResult(error);
                context.ExceptionHandled = true;
            }
        }

        public string ReadAllBody(HttpContext httpContext)
        {
            httpContext.Request.Body.Seek(0, System.IO.SeekOrigin.Begin);
            return Encoding.UTF8.GetString(httpContext.Request.Body.ReadAllBytes());
        }

        private object TryGetProp(HttpContext context, string key)
        {
            object result = null;
            context.Items.TryGetValue(key, out result);
            return result;
        }

        private HttpStatusCode GetStatus(Type type)
        {
            if (type == typeof(UnauthorizedAccessException))
                return HttpStatusCode.Forbidden;

            if (type == typeof(AuthenticationException))
                return HttpStatusCode.Unauthorized;

            if (type == typeof(EntityNotFoundException))
                return HttpStatusCode.NotFound;

            if (type == typeof(IntegrityCheckException))
                return HttpStatusCode.BadRequest;

            return HttpStatusCode.InternalServerError;
        }
    }

    public class HttpError
    {
        public HttpError(Exception e)
        {
            this.ExceptionMessage = e.Message;
            this.ExceptionType = e.GetType().FullName;
            this.StackTrace = e.StackTrace;
            this.ExceptionId = e.GetExceptionEntity()?.Id.ToString();
            this.InnerException = e.InnerException == null ? null : new HttpError(e.InnerException);
        }

        public string ExceptionId;
        public string ExceptionMessage;
        public string ExceptionType;
        public string StackTrace;
        public HttpError InnerException;
    }
}