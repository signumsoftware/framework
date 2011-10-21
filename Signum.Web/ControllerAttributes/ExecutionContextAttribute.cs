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
        public static Func<ActionExecutingContext, ExecutionContext>  SetExecutionContext = a=> ExecutionContext.UserInterface;

     
        public ExecutionContextAttribute()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if(SetExecutionContext != null)
            {
                ExecutionContext context = SetExecutionContext(filterContext);

                filterContext.RequestContext.HttpContext.Items[typeof(ExecutionContext)] = ExecutionContext.Scope(context);
            }
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (SetExecutionContext != null)
            {
                var oldContext = ExecutionContext.Current;
                IDisposable scope = (IDisposable)filterContext.RequestContext.HttpContext.Items[typeof(ExecutionContext)];
                if (scope != null)
                {
                    scope.Dispose();
                    filterContext.RequestContext.HttpContext.Items.Remove(typeof(ExecutionContext));
                }
            }
        }

    }
}