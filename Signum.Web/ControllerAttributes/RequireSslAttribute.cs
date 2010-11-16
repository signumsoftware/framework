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
        public void OnActionExecuted(ActionExecutedContext filterContext) { }
    }
}