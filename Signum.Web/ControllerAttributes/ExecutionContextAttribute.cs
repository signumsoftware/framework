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

        //static StringBuilder sb = new StringBuilder(); 

        public ExecutionContextAttribute()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if(SetExecutionContext != null)
            {
                var context = SetExecutionContext(filterContext);

                //sb.AppendFormat(System.Threading.Thread.CurrentThread.ManagedThreadId +  " Start {0}.{1}  {2}->{3}\r\n",
                //    filterContext.ActionDescriptor.ActionName,
                //    filterContext.ActionDescriptor.ControllerDescriptor.ControllerName,
                //    ExecutionContext.Current.TryToString() ?? "NULL", 
                //    context.TryToString() ?? "NULL"); 

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

                //sb.AppendFormat(System.Threading.Thread.CurrentThread.ManagedThreadId + " End {0}.{1} {2}->{3}  {4}\r\n",
                //       filterContext.RouteData.Values["Action"],
                //       filterContext.RouteData.Values["Controller"],
                //       oldContext.TryToString() ?? "NULL",
                //       ExecutionContext.Current.TryToString() ?? "NULL",
                //       scope == null ? "NO SCOPE!!" : "");
            }
        }

    }
}