using Signum.Engine;
using Signum.Engine.Basics;
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

        public static string GetActionDescription(HttpActionContext actionContext)
        {
            var action = actionContext.ActionDescriptor.ControllerDescriptor.ControllerName + "." + actionContext.ActionDescriptor.ActionName;

            var rad = actionContext.ActionDescriptor as ReflectedHttpActionDescriptor;
            if (rad == null)
            {
                var attr = rad.MethodInfo.GetCustomAttributes(true).OfType<ProfilerActionSplitterAttribute>().FirstOrDefault();
                if (attr != null)
                {
                    var obj = attr.RequestKey == null ? actionContext.ActionArguments.Values.Single() : actionContext.ActionArguments.GetOrThrow(attr.RequestKey, "Argument '{0}' not found in: " + rad.MethodInfo.MethodSignature());

                    if (obj != null)
                        action += " " + obj.ToString();
                }
            }

            return action;
        }
    }
}