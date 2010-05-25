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

namespace Signum.Web
{
    /// <summary>
    /// Obtenido de RequireSslAttribute ChangeSet 23011 ASP.NET  15/07/2009
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class RequireSslAttribute : FilterAttribute, IActionFilter
    {
        public bool Redirect { get; set; }

        public bool Www { get; set; }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (!filterContext.HttpContext.Request.IsSecureConnection && !AppSettings.ReadBoolean(AppSettingsKeys.Development, false))
            {
                // request is not SSL-protected, so throw or redirect
                if (Redirect)
                {
                    // form new URL
                    UriBuilder builder = new UriBuilder()
                    {
                        Scheme = "https",
                        Host = (Www && !filterContext.HttpContext.Request.Url.Host.StartsWith("www")
                    && !filterContext.HttpContext.Request.Url.Host.StartsWith("localhost")) ?

                    "www." + filterContext.HttpContext.Request.Url.Host :
                        filterContext.HttpContext.Request.Url.Host,
                        // use the RawUrl since it works with URL Rewriting
                        Path = filterContext.HttpContext.Request.RawUrl
                    };
                    filterContext.Result = new RedirectResult(builder.ToString());
                }
                else
                {
                    throw new HttpException((int)HttpStatusCode.Forbidden,
                        "RequireSslAttribute debe usar SSL");
                    /*MvcResources.RequireSslAttribute_MustUseSsl*/
                }
            }
        }
        public void OnActionExecuted(ActionExecutedContext filterContext){}
    }

    /// <summary>
    /// Obtenido de RequireSslAttribute ChangeSet 23011 ASP.NET  15/07/2009
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class RedirectToUrl : FilterAttribute, IActionFilter
    {
        public string Url
        {
            get;
            set;
        }

        public RedirectToUrl(string url)
        {
            this.Url = url;
        }
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }
            filterContext.Result = new RedirectResult(Url);
        }
        public void OnActionExecuted(ActionExecutedContext filterContext){}
    }

    /// <summary>
    /// Muestra u oculta los botones de navegación inferiores
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NavigationButtonsAttribute : FilterAttribute, IActionFilter
    {
        public bool Show
        {
            get;
            set;
        }
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.Controller.ViewData[ViewDataKeys.NavigationButtons] = Show;
        }


        #region IActionFilter Members

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Checks the User's authentication using FormsAuthentication
    /// and redirects to the Login Url for the application on fail
    /// </summary>
    public class AuthenticationRequiredAttribute : AuthorizeAttribute
    {
        public static Action<AuthorizationContext> Authenticate = context =>
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                //use the current url for the redirect
                string redirectOnSuccess = context.HttpContext.Request.Url.AbsolutePath;
                //send them off to the login page
                string redirectUrl = string.Format("?ReturnUrl={0}", redirectOnSuccess);
                //string loginUrl = context.HttpContext.Request.UrlReferrer + "Auth/Login" + redirectUrl;
                string loginUrl = HttpContextUtils.FullyQualifiedApplicationPath + "Auth/Login" + redirectUrl;
                context.HttpContext.Response.Redirect(loginUrl, true);
            }
        };

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (Authenticate != null)
                Authenticate(filterContext);     
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
        Justification = "This attribute is AllowMultiple = true and users might want to override behavior.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    [AspNetHostingPermission(System.Security.Permissions.SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class HandleExceptionAttribute : HandleErrorAttribute
    {
        public static Action<HandleErrorInfo> LogControllerException;
        public static Action<Exception> LogGlobalException;

        public override void OnException(ExceptionContext filterContext)
        {
            Exception exception = filterContext.Exception;
            string controllerName = (string)filterContext.RouteData.Values["controller"];
            string actionName = (string)filterContext.RouteData.Values["action"];
            HandleErrorInfo model = new HandleErrorInfo(filterContext.Exception, controllerName, actionName);

            if (LogControllerException != null) LogControllerException(model);

            
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
                        {ViewDataKeys.PageTitle, model.Exception.InnerException != null ? 
                            model.Exception.InnerException.Message : 
                            model.Exception.Message}
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

            if (error == 401) //not authoriozed shows a log-io
                return 500;
            return error;
        }

        public static string ErrorSessionKey = "sfError";

        public static void HandlerApplication_Error(HttpContext context)
        {
            if (Navigator.Manager == null || !Navigator.Manager.Started)
                return;

            Exception ex = context.Server.GetLastError();
            context.Server.ClearError();

            context.Response.StatusCode = GetHttpError(ex);

            if (LogGlobalException != null) LogGlobalException(ex);

            context.Session[ErrorSessionKey] = ex;

            var httpContext = HttpContext.Current;

            UrlHelper helper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current),
                RouteTable.Routes.GetRouteData(new HttpContextWrapper(HttpContext.Current))));  //Change in ASP.Net MVC 2

            httpContext.RewritePath(helper.Action("Error", "Signum"), false);
            IHttpHandler httpHandler = new MvcHttpHandler();
            httpHandler.ProcessRequest(HttpContext.Current);
        }
    }
}
