using System;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Linq.Expressions;
using System.Web.Routing;
using System.Reflection;

namespace Signum.Web
{
    public static class RenderActionExtenders
    {
        public static MvcHtmlString RenderActionToString<TController>(this HtmlHelper helper, HttpRequest request, Expression<Action<TController>> action)
            where TController : Controller
        {
            //Create memory writer
            var sb = new StringBuilder();
            var memWriter = new StringWriter(sb);

            //Create fake http context to render the view
            var fakeResponse = new HttpResponse(memWriter);
            var fakeContext = new HttpContext(request, fakeResponse);
            var fakeControllerContext = new ControllerContext(
                new HttpContextWrapper(fakeContext),
                helper.ViewContext.RouteData,
                helper.ViewContext.Controller);

            var oldContext = HttpContext.Current;
            HttpContext.Current = fakeContext;

            //Use HtmlHelper to render partial view to fake context
            var html = new HtmlHelper(new ViewContext(fakeControllerContext,
                new Signum.Web.RenderPartialExtenders.FakeView(), new ViewDataDictionary(), new TempDataDictionary(), memWriter),
                new ViewPage());
            html.RenderAction<TController>(action);

            //Restore context
            HttpContext.Current = oldContext;

            //Flush memory and return output
            memWriter.Flush();
            return MvcHtmlString.Create(sb.ToString());
        }

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

    public static class RenderPartialExtenders
    {
        public static MvcHtmlString RenderPartialToString(this HtmlHelper helper,
                                        string viewName, ViewDataDictionary viewData)
        {
            //Create memory writer
            var sb = new StringBuilder();
            var memWriter = new StringWriter(sb);

            //Create fake http context to render the view
            var fakeResponse = new HttpResponse(memWriter);
            var fakeContext = new HttpContext(HttpContext.Current.Request, fakeResponse);
            var fakeControllerContext = new ControllerContext(
                new HttpContextWrapper(fakeContext),
                helper.ViewContext.RouteData,
                helper.ViewContext.Controller);

            var oldContext = HttpContext.Current;
            HttpContext.Current = fakeContext;

            //Use HtmlHelper to render partial view to fake context
            var html = new HtmlHelper(new ViewContext(fakeControllerContext,
                new FakeView(), new ViewDataDictionary(), new TempDataDictionary(), memWriter),
                new ViewPage());
            html.RenderPartial(viewName, viewData);

            //Restore context
            HttpContext.Current = oldContext;

            //Flush memory and return output
            memWriter.Flush();
            return MvcHtmlString.Create(sb.ToString());
        }

        public class FakeView : IView
        {
            #region IView Members
            public void Render(ViewContext viewContext, System.IO.TextWriter writer)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
    }

    public static class ExpressionHelper
    {
        public static RouteValueDictionary GetRouteValuesFromExpression<TController>(Expression<Action<TController>> action) where TController : Controller
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            MethodCallExpression call = action.Body as MethodCallExpression;
            if (call == null)
            {
                throw new ArgumentException("Action must be a method call", "action");
            }

            string controllerName = typeof(TController).Name;
            if (!controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Action target must end in controller", "action");
            }
            controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);
            if (controllerName.Length == 0)
            {
                throw new ArgumentException("Action cannot route to controller", "action");
            }

            // TODO: How do we know that this method is even web callable?
            //      For now, we just let the call itself throw an exception.

            var rvd = new RouteValueDictionary();
            rvd.Add("Controller", controllerName);
            rvd.Add("Action", call.Method.Name);
            AddParameterValuesFromExpressionToDictionary(rvd, call);
            return rvd;
        }

        public static string GetInputName<TModel, TProperty>(Expression<Func<TModel, TProperty>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.Call)
            {
                MethodCallExpression methodCallExpression = (MethodCallExpression)expression.Body;
                string name = GetInputName(methodCallExpression);
                return name.Substring(expression.Parameters[0].Name.Length + 1);

            }
            return expression.Body.ToString().Substring(expression.Parameters[0].Name.Length + 1);
        }

        private static string GetInputName(MethodCallExpression expression)
        {
            // p => p.Foo.Bar().Baz.ToString() => p.Foo OR throw...

            MethodCallExpression methodCallExpression = expression.Object as MethodCallExpression;
            if (methodCallExpression != null)
            {
                return GetInputName(methodCallExpression);
            }
            return expression.Object.ToString();
        }

        static void AddParameterValuesFromExpressionToDictionary(RouteValueDictionary rvd, MethodCallExpression call)
        {
            ParameterInfo[] parameters = call.Method.GetParameters();

            if (parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    Expression arg = call.Arguments[i];
                    object value = null;
                    ConstantExpression ce = arg as ConstantExpression;
                    if (ce != null)
                    {
                        // If argument is a constant expression, just get the value
                        value = ce.Value;
                    }
                    else
                    {
                        // Otherwise, convert the argument subexpression to type object,
                        // make a lambda out of it, compile it, and invoke it to get the value
                        Expression<Func<object>> lambdaExpression = Expression.Lambda<Func<object>>(Expression.Convert(arg, typeof(object)));
                        Func<object> func = lambdaExpression.Compile();
                        value = func();
                    }
                    rvd.Add(parameters[i].Name, value);
                }
            }
        }

    }
}