using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Net;
using System.Web;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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


    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
        Justification = "This attribute is AllowMultiple = true and users might want to override behavior.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    [AspNetHostingPermission(System.Security.Permissions.SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class HandleExceptionAttribute : HandleErrorAttribute
    {
        public static Action<HandleErrorInfo> LogException;

        public override void OnException(ExceptionContext filterContext)
        {
            //TODO:Añadir logging
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            // If custom errors are disabled, we need to let the normal ASP.NET exception handler
            // execute so that the user can see useful debugging information.
            /*if (filterContext.ExceptionHandled || !filterContext.HttpContext.IsCustomErrorEnabled)
            {
                return;
            }*/

            Exception exception = filterContext.Exception;

            // If this is not an HTTP 500 (for example, if somebody throws an HTTP 404 from an action method),
            // ignore it.
            if (new HttpException(null, exception).GetHttpCode() != 500)
            {
                return;
            }

            if (!ExceptionType.IsInstanceOfType(exception))
            {
                return;
            }

            string controllerName = (string)filterContext.RouteData.Values["controller"];
            string actionName = (string)filterContext.RouteData.Values["action"];
            HandleErrorInfo model = new HandleErrorInfo(filterContext.Exception, controllerName, actionName);

            if (LogException != null) LogException(model);

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
                    ViewData = new ViewDataDictionary<HandleErrorInfo>(model),
                    TempData = filterContext.Controller.TempData
                };
                ((ViewResult)filterContext.Result).ViewData[ViewDataKeys.PageTitle] = model.Exception.Message;
            }
            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.Clear();
            filterContext.HttpContext.Response.StatusCode = 500;
        }
    }

   // [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
   // public class HandleUnknownActionAttribute : Han 
}
