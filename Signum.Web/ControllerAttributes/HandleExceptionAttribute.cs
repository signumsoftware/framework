using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Net;
using System.Web;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Web.Security;
using Signum.Utilities;
using System.Web.Routing;
using Signum.Engine.Exceptions;

namespace Signum.Web
{
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
           Justification = "This attribute is AllowMultiple = true and users might want to override behavior.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    [AspNetHostingPermission(System.Security.Permissions.SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class HandleExceptionAttribute : HandleErrorAttribute
    {
        public static Action<ExceptionContext> OnExceptionHandled;
        public static Action<HandleErrorInfo> LogControllerException;
        public static Action<Exception> LogGlobalException;

        public override void OnException(ExceptionContext filterContext)
        {
            if (OnExceptionHandled != null)
                OnExceptionHandled(filterContext);
            else
                DefaultOnException(filterContext);
        }

        public static void DefaultOnException(ExceptionContext filterContext)
        {
            Exception exception = filterContext.Exception.FollowC(a => a.InnerException).Last();
            string controllerName = (string)filterContext.RouteData.Values["controller"];
            string actionName = (string)filterContext.RouteData.Values["action"];
            HandleErrorInfo model = new HandleErrorInfo(exception, controllerName, actionName);

            if (LogControllerException != null)
                LogControllerException(model);

            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                //we do not want to use a master page, just render a control with the error string
                filterContext.Result = new PartialViewResult
                {
                    ViewName = Navigator.Manager.AjaxErrorPageUrl,
                    ViewData = new ViewDataDictionary<HandleErrorInfo>(model),
                    TempData = filterContext.Controller.TempData
                };
            }
            else
            {
                filterContext.Result = new ViewResult
                {
                    ViewName = Navigator.Manager.ErrorPageUrl,
                    TempData = filterContext.Controller.TempData,
                    ViewData = new ViewDataDictionary<HandleErrorInfo>(model)
                    {
                        {ViewDataKeys.Title, model.Exception.InnerException != null ?  model.Exception.InnerException.Message : model.Exception.Message}
                    },
                };
            }
            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.Clear();
            filterContext.HttpContext.Response.StatusCode = GetHttpError(exception);
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
        }

        public static int GetHttpError(Exception ex)
        {
            int error = new HttpException(null, ex).GetHttpCode();

            if (error == 401) //not authorized shows a log-io
                return 500;

            if (ex.GetType() == typeof(EntityNotFoundException))
                return 404;
            return error;
        }

        public static string ErrorSessionKey = "sfError";

        public static void HandlerApplication_Error(HttpContext context, bool isWebRequest)
        {
            if (Navigator.Manager == null || !Navigator.Manager.Initialized)
                return;

            Exception ex = context.Server.GetLastError().FollowC(a => a.InnerException).Last();
            context.Server.ClearError();

            context.Response.StatusCode = GetHttpError(ex);
            context.Response.TrySkipIisCustomErrors = true;

            if (LogGlobalException != null) LogGlobalException(ex);

            if (isWebRequest)
            {
                context.Session[ErrorSessionKey] = ex;

                var httpContext = HttpContext.Current;

                UrlHelper helper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current),
                    RouteTable.Routes.GetRouteData(new HttpContextWrapper(HttpContext.Current))));  //Change in ASP.Net MVC 2

                httpContext.RewritePath(helper.Action("Error", "Error"), false);
                IHttpHandler httpHandler = new MvcHttpHandler();
                httpHandler.ProcessRequest(HttpContext.Current);
            }
        }
    }
}