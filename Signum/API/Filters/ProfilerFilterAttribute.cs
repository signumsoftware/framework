using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Signum.API.Filters;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class ProfilerActionSplitterAttribute : Attribute
{
    public string RequestKey { get; }

    public ProfilerActionSplitterAttribute(string requestKey)
    {
        this.RequestKey = requestKey;
    }

    public static string GetActionDescription(FilterContext actionContext)
    {
        var cad = (ControllerActionDescriptor)actionContext.ActionDescriptor;

        var action = cad.ControllerName + "." + cad.ActionName;

        var splitter = actionContext.ActionDescriptor.EndpointMetadata.OfType<ProfilerActionSplitterAttribute>().FirstOrDefault();
        if (splitter != null)
        {
            var obj = actionContext.RouteData.Values.TryGetCN(splitter.RequestKey) ??
                actionContext.HttpContext.Request.Query[splitter.RequestKey].Only() ??
                throw new InvalidOperationException("Argument '{0}' not found in: " + cad.MethodInfo.MethodSignature());

            if (obj != null)
                action += " " + obj.ToString();
        }

        return action;
    }
}
