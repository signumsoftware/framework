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
using Signum.Engine.Linq;
using Signum.Engine;

namespace Signum.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class UserInterfaceAttribute : FilterAttribute, IActionFilter
    {
        public UserInterfaceAttribute()
        {
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Database.SetUserInterface(true); 
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            Database.SetUserInterface(false); 
        }
    }
}