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
    public sealed class ExecutionContextAttribute : ActionFilterAttribute
    {
        public static Func<ControllerContext, ExecutionContext> SetExecutionContext = a => ExecutionContext.UserInterface;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ExecutionContext context = SetExecutionContext(filterContext);

            filterContext.Controller.ViewData.Add("ExecutionContext", ExecutionContext.Scope(context));
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            IDisposable scope = (IDisposable)filterContext.Controller.ViewData["ExecutionContext"];
            if (scope != null)
            {
                scope.Dispose();
                filterContext.Controller.ViewData.Remove("ExecutionContext");
            }
        }
    }
}