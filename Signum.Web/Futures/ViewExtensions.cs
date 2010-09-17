namespace Signum.Web
{
    using System;
    using System.Linq.Expressions;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;

    public static class ViewExtensions
    {

        public static void RenderRoute(this HtmlHelper helper, RouteValueDictionary routeValues)
        {
            var routeData = new RouteData();
            foreach (var kvp in routeValues)
            {
                routeData.Values.Add(kvp.Key, kvp.Value);
            }
            var httpContext = helper.ViewContext.HttpContext;
            var requestContext = new RequestContext(httpContext, routeData);
            var handler = new RenderActionMvcHandler(requestContext);
            handler.ProcessRequestInternal(httpContext);
        }

        public static void RenderAction<TController>(this HtmlHelper helper, Expression<Action<TController>> action) where TController : Controller
        {
            RouteValueDictionary rvd = ExpressionHelper.GetRouteValuesFromExpression(action);
            helper.RenderRoute(rvd);
        }

        public static void RenderAction(this HtmlHelper helper, string actionName, string controllerName, object routeValues)
        {
            helper.RenderAction(actionName, controllerName, new RouteValueDictionary(routeValues));
        }

        public static void RenderAction(this HtmlHelper helper, string actionName, string controllerName, RouteValueDictionary routeValues)
        {
            RouteValueDictionary rvd = null;
            if (routeValues != null)
            {
                rvd = new RouteValueDictionary(routeValues);
            }
            else
            {
                rvd = new RouteValueDictionary();
            }

            foreach (var entry in helper.ViewContext.RouteData.Values)
            {
                if (!rvd.ContainsKey(entry.Key))
                {
                    rvd.Add(entry.Key, entry.Value);
                }
            }

            if (!string.IsNullOrEmpty(actionName))
            {
                rvd["action"] = actionName;
            }
            if (!string.IsNullOrEmpty(controllerName))
            {
                rvd["controller"] = controllerName;
            }

            helper.RenderRoute(rvd);
        }

        public static void RenderAction(this HtmlHelper helper, string actionName, string controllerName)
        {
            helper.RenderAction(actionName, controllerName, null);
        }

        public static void RenderAction(this HtmlHelper helper, string actionName)
        {
            helper.RenderAction(actionName, null);
        }

        private class RenderActionMvcHandler : MvcHandler
        {
            public RenderActionMvcHandler(RequestContext context)
                : base(context)
            {
            }

            protected override void AddVersionHeader(HttpContextBase httpContext)
            {
                // Don't try to set the version header when rendering actions
            }

            public void ProcessRequestInternal(HttpContextBase httpContext)
            {
                base.ProcessRequest(httpContext);
            }
        }
    }
}
