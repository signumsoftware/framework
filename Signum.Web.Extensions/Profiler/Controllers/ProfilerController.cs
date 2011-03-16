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

namespace Signum.Web.Profiler
{
    public class ProfilerController : Controller
    {
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Heavy()
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            ViewData[ViewDataKeys.Title] = "Root entries";

            return View(ProfilerClient.ViewPrefix.Formato("HeavyList"), Signum.Utilities.HeavyProfiler.Entries); 
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult HeavySlowest(int? top)
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            var list = Signum.Utilities.HeavyProfiler.AllEntries().Where(a => a.Role == "SQL").OrderByDescending(a => a.Elapsed).Take(top ?? 50).ToList();

            ViewData[ViewDataKeys.Title] = "Slowest SQL entries"; 

            return View(ProfilerClient.ViewPrefix.Formato("HeavyList"), list);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult HeavyRoute(string indices)
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            var entry = HeavyProfiler.Find(indices);

            ViewData[ViewDataKeys.Title] = "Entry " + entry.FullIndex(); 

            return View(ProfilerClient.ViewPrefix.Formato("HeavyDetails"), entry);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Enable()
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            Signum.Utilities.HeavyProfiler.Enabled = true;
            return RedirectToAction("Heavy");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Disable()
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            Signum.Utilities.HeavyProfiler.Enabled = false;
            return RedirectToAction("Heavy");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Clean()
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            Signum.Utilities.HeavyProfiler.Clean();
            return RedirectToAction("Heavy");
        }


        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Times(int? clear)
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
        public ActionResult TimeTable(int? clear)
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
    }
}
