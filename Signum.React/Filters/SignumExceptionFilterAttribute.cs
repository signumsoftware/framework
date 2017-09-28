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
using System.ServiceModel.Channels;
using System.Web;
using System.Web.Http;
using System.Web.Http.Filters;

namespace Signum.React.Filters
{
    public class SignumExceptionFilterAttribute : ExceptionFilterAttribute
    {
        static Func<HttpActionExecutedContext, bool> IncludeErrorDetails = ctx => true;

        public static readonly List<Type> IgnoreExceptions = new List<Type> { typeof(OperationCanceledException) };

        public override void OnException(HttpActionExecutedContext ctx)
        {
            if (!IgnoreExceptions.Contains(ctx.Exception.GetType()))
            {
                var statusCode = GetStatus(ctx.Exception.GetType());

                var error = new HttpError(ctx.Exception, IncludeErrorDetails(ctx));

                var req = ctx.Request;

                var exLog = ctx.Exception.LogException(e =>
                {
                    e.ActionName = ctx.ActionContext.ActionDescriptor.ActionName;
                    e.ControllerName = ctx.ActionContext.ActionDescriptor.ControllerDescriptor.ControllerName;
                    e.UserAgent = req.Headers.UserAgent.ToString();
                    e.RequestUrl = req.RequestUri.ToString();
                    e.UrlReferer = req.Headers.Referrer?.ToString();
                    e.UserHostAddress = GetClientIp(req);
                    e.UserHostName = GetClientName(req);
                    e.User = (UserHolder.Current ?? (IUserEntity)GetProp(req, SignumAuthenticationFilterAttribute.UserKey))?.ToLite();
                    e.QueryString = ExceptionEntity.Dump(req.RequestUri.ParseQueryString());
                    e.Form =  (string)GetProp(req, SignumAuthenticationFilterAttribute.SavedRequestKey);
                    e.Session = GetSession(req);
                });

                error["ExceptionID"] = exLog.Id.ToString();

                ctx.Response = ctx.Request.CreateResponse<HttpError>(statusCode, error);
            }

            base.OnException(ctx);
        }

     

        private object GetProp(HttpRequestMessage req, string key)
        {
            object result = null;
            req.Properties.TryGetValue(key, out result);
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

        public static string GetClientIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }

        public static string GetClientName(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostName;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostName;
            }
            else
            {
                return null;
            }
        }


        private string GetSession(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                var ses = ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Session;
                return ses == null ? "[No Session]" : ses.Cast<string>().ToString(key => key + ": " + ses[key].Dump(), "\r\n");
            }
            else if (HttpContext.Current != null)
            {
                var ses = HttpContext.Current.Session;
                return ses == null ? "[No Session]" : ses.Cast<string>().ToString(key => key + ": " + ses[key].Dump(), "\r\n");
            }
            else
            {
                return null;
            }
        }

    }
}