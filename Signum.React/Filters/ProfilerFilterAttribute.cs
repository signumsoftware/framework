using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Linq;

namespace Signum.React.Filters
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class ProfilerActionSplitterAttribute : Attribute
    {
        readonly string? requestKey;

        public ProfilerActionSplitterAttribute(string? requestKey = null)
        {
            this.requestKey = requestKey;
        }

        public string? RequestKey
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
