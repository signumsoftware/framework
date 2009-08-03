using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Net;
using System.Web;

namespace Signum.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class WwwRedirectAttribute : FilterAttribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (!filterContext.HttpContext.Request.Url.Host.StartsWith("www")
                    && !filterContext.HttpContext.Request.Url.Host.StartsWith("localhost"))
            {

                // form new URL
                UriBuilder builder = new UriBuilder()
                {
                    Scheme = filterContext.HttpContext.Request.Url.Scheme,
                    Host = "www." + filterContext.HttpContext.Request.Url.Host,
                    // use the RawUrl since it works with URL Rewriting
                    Path = filterContext.HttpContext.Request.RawUrl
                };
                filterContext.Result = new RedirectResult(builder.ToString());
            }
        }

        public void OnActionExecuted(ActionExecutedContext filterContext) {
        }
    }
}