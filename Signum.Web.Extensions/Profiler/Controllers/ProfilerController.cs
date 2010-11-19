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
    [HandleException, AuthenticationRequired]
    public class ProfilerController : Controller
    {
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Heavy()
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            return View(ProfileClient.ViewPath + "HeavyList", Signum.Utilities.HeavyProfiler.Entries); 
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult HeavyRoute(string indices)
        {
            ProfilerPermissions.ViewHeavyProfiler.Authorize();

            int[] ind = indices.Split(',').Select(a => int.Parse(a)).ToArray(); 

            HeavyProfilerDetails details = new HeavyProfilerDetails
            {
                Indices = ind,
                Previous = new List<HeavyProfilerEntry>(),
                Entry = null, 
            };

            List<HeavyProfilerEntry> currentList = Signum.Utilities.HeavyProfiler.Entries;
            for (int i = 0; i < ind.Length; i++)
            {
                if (i != 0)
                    details.Previous.Add(details.Entry);

                int index = ind[i];

                if (currentList == null || currentList.Count <= index)
                    throw new InvalidOperationException("The ProfileEntry is not available"); 

                details.Entry = currentList[index];

                currentList = details.Entry.Entries;
            }

            return View(ProfileClient.ViewPath + "HeavyDetails", details);
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

            if (clear != null && clear == 1)
                Signum.Utilities.TimeTracker.IdentifiedElapseds = new Dictionary<string, Signum.Utilities.TimeTrackerEntry>();
            ViewData[ViewDataKeys.PageTitle] = "Times";
            return View(ProfileClient.ViewPath + "Times");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult TimeTable(int? clear)
        {
            ProfilerPermissions.ViewTimeTracker.Authorize();

            if (clear != null && clear == 1)
                Signum.Utilities.TimeTracker.IdentifiedElapseds = new Dictionary<string, Signum.Utilities.TimeTrackerEntry>();
            ViewData[ViewDataKeys.PageTitle] = "Table Times";
            return View(ProfileClient.ViewPath + "TimeTable");
        }
    }

    public class HeavyProfilerDetails
    {
        public int[] Indices; 
        public HeavyProfilerEntry Entry;
        public List<HeavyProfilerEntry> Previous;  
    }
}
