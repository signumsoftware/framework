using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web.Profiler
{
    public class TrackTimeFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var action = filterContext.RouteData.Values["controller"] + "." + filterContext.RouteData.Values["action"];

            filterContext.Controller.ViewData["elapsed"] = TimeTracker.Start(action);

            IDisposable profiler = HeavyProfiler.Log(aditionalData: action);
            if (profiler != null)
                filterContext.Controller.ViewData["profiler"] = profiler;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Result == null)
            {
                IDisposable elapsed = (IDisposable)filterContext.Controller.ViewData.TryGetC("elapsed");
                if (elapsed != null)
                    elapsed.Dispose();
            }

            IDisposable profiler = (IDisposable)filterContext.Controller.ViewData.TryGetC("profiler");
            if (profiler != null)
                profiler.Dispose();
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            IDisposable elapsed = (IDisposable)filterContext.Controller.ViewData.TryGetC("elapsed");
            if (elapsed != null)
                elapsed.Dispose();
        }
    }
}