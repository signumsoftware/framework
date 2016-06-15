using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Profiler;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.ServiceModel.Channels;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Routing;

namespace Signum.React.Filters
{
    
    public class ProfilerFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var routeData = actionContext.ControllerContext.RouteData.Values;

            var action = actionContext.ActionDescriptor.ControllerDescriptor.ControllerName + "." + actionContext.ActionDescriptor.ActionName;

            var rad = actionContext.ActionDescriptor as ReflectedHttpActionDescriptor;
            if (rad != null)
            {
                var attr = rad.MethodInfo.GetCustomAttributes(true).OfType<ProfilerActionSplitterAttribute>().FirstOrDefault();
                if (attr != null)
                {
                    var obj = attr.RequestKey == null ? actionContext.ActionArguments.Values.Single() : actionContext.ActionArguments.GetOrThrow(attr.RequestKey, "Argument '{0}' not found in: " + rad.MethodInfo.MethodSignature());

                    if (obj != null)
                        action += " " + obj.ToString();
                }
            }


            routeData.Add("elapsed", TimeTracker.Start(action));

            IDisposable profiler = HeavyProfiler.Log("Web.API " + actionContext.Request.Method, () => actionContext.Request.RequestUri.ToString());
            if (profiler != null)
                routeData.Add("profiler", profiler);


            if (ProfilerLogic.SessionTimeout != null)
            {
                IDisposable sessionTimeout = Connector.CommandTimeoutScope(ProfilerLogic.SessionTimeout.Value);
                if (sessionTimeout != null)
                    routeData.Add("sessiontimeout", sessionTimeout);
            }
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {

            var routeData = actionExecutedContext.Request.GetRouteData();

            Dispose(routeData, "profiler");
            Dispose(routeData, "elapsed");
            Dispose(routeData, "sessiontimeout");

            base.OnActionExecuted(actionExecutedContext);
        }


        private void Dispose(IHttpRouteData routeData, string key)
        {
            IDisposable elapsed = (IDisposable)routeData.Values.TryGetC(key);
            if (elapsed != null)
            {
                elapsed.Dispose();
                routeData.Values.Remove(key);
            }
        }
    }


    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class ProfilerActionSplitterAttribute : Attribute
    {
        readonly string requestKey;

        public ProfilerActionSplitterAttribute(string requestKey = null)
        {
            this.requestKey = requestKey;
        }

        public string RequestKey
        {
            get { return requestKey; }
        }
    }
}