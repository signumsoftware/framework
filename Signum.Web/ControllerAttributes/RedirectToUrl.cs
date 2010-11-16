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
    public sealed class RedirectToUrl : FilterAttribute, IActionFilter
    {
        public string Url { get; set; }

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

        public void OnActionExecuted(ActionExecutedContext filterContext) { }
    }
}