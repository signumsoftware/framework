using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
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
using System.Text;
using System.Web;
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

        public static string GetActionDescription(FilterContext actionContext)
        {
            var cad = (ControllerActionDescriptor)actionContext.ActionDescriptor;

            var action = cad.ControllerName + "." + cad.ActionName;

            if (cad == null)
            {
                var attr = cad.MethodInfo.GetCustomAttributes(true).OfType<ProfilerActionSplitterAttribute>().FirstOrDefault();
                if (attr != null)
                {
                    var obj = attr.RequestKey == null ? null : actionContext.ActionDescriptor.RouteValues.GetOrThrow(attr.RequestKey, "Argument '{0}' not found in: " + cad.MethodInfo.MethodSignature());

                    if (obj != null)
                        action += " " + obj.ToString();
                }
            }

            return action;
        }
    }
}