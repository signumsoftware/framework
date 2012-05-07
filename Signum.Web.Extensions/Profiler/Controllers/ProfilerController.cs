using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.Web.Extensions.Properties;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Linq;
using Signum.Entities;
using Signum.Web.Controllers;
using System.Collections.Generic;
using Signum.Entities.Profiler;
using Signum.Engine.Profiler;

namespace Signum.Web.Profiler
{
  

    public class ProfilerController : Controller
    {
        public ActionResult Heavy()
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            ViewData[ViewDataKeys.Title] = "Root entries";

            if (Request.IsAjaxRequest())
                return PartialView(ProfilerClient.ViewPrefix.Formato("ProfilerTable"), HeavyProfiler.Entries); 
            else
                return View(ProfilerClient.ViewPrefix.Formato("HeavyList"), HeavyProfiler.Entries); 
          
        }

        public ActionResult Statistics(SqlProfileResumeOrder order)
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            var list = HeavyProfiler.SqlStatistics().ToList().OrderByDescending(GetOrder(order)); ;

            ViewBag.Order = order;
            ViewData[ViewDataKeys.Title] = "Slowest SQLs";

            if (Request.IsAjaxRequest())
                return PartialView(ProfilerClient.ViewPrefix.Formato("StatisticsTable"), list);
            else
                return View(ProfilerClient.ViewPrefix.Formato("Statistics"), list);
        }

        private Func<SqlProfileResume, long> GetOrder(SqlProfileResumeOrder order)
        {
            switch (order)
            {
                case SqlProfileResumeOrder.Count: return o => o.Count;
                case SqlProfileResumeOrder.Sum: return o => o.Sum.Ticks;
                case SqlProfileResumeOrder.Avg: return o => o.Avg.Ticks;
                case SqlProfileResumeOrder.Min: return o => o.Min.Ticks;
                case SqlProfileResumeOrder.Max: return o => o.Max.Ticks;
            }
            throw new InvalidOperationException();
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult HeavyRoute(string indices)
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            var entry = HeavyProfiler.Find(indices);

            ViewData[ViewDataKeys.Title] = "Entry " + entry.FullIndex(); 

            return View(ProfilerClient.ViewPrefix.Formato("HeavyDetails"), entry);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Enable()
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            Signum.Utilities.HeavyProfiler.Enabled = true;
            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Disable()
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            Signum.Utilities.HeavyProfiler.Enabled = false;
            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Clean()
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            Signum.Utilities.HeavyProfiler.Clean();
            return null;
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Times()
        {
            ProfilerPermissions.ViewTimeTracker.Authorize();

            return View(ProfilerClient.ViewPrefix.Formato("Times"));
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ClearTimes()
        {
            ProfilerPermissions.ViewTimeTracker.Authorize();

            Signum.Utilities.TimeTracker.IdentifiedElapseds.Clear();
            return RedirectToAction("Times"); 
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult TimeTable()
        {
            ProfilerPermissions.ViewTimeTracker.Authorize();

            return View(ProfilerClient.ViewPrefix.Formato("TimeTable"));
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ClearTimesTable()
        {
            ProfilerPermissions.ViewTimeTracker.Authorize();

            Signum.Utilities.TimeTracker.IdentifiedElapseds.Clear();
            return RedirectToAction("Times");
        }


        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult OverrideSessionTimeout(int? sec)
        {
            ProfilerPermissions.OverrideSessionTimeout.Authorize();

            ProfilerLogic.SessionTimeout = sec;

            return Content("Session Timeout overriden to {0} seconds".Formato(ProfilerLogic.SessionTimeout));
        }

    }

    public enum SqlProfileResumeOrder
    {
        Count,
        Sum,
        Avg,
        Min,
        Max
    }
}
