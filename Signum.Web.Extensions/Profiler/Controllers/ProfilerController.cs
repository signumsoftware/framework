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
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Linq;
using Signum.Entities;
using Signum.Web.Controllers;
using System.Collections.Generic;
using Signum.Entities.Profiler;
using Signum.Engine.Profiler;
using System.Xml.Linq;
using System.IO;

namespace Signum.Web.Profiler
{
    public class ProfilerController : Controller
    {
        public ActionResult Heavy(bool orderByTime = false)
        {
            ProfilerPermission.ViewHeavyProfiler.Authorize();

            ViewData[ViewDataKeys.Title] = "Root entries";

            ViewBag.OrderByTime = orderByTime;

            List<HeavyProfilerEntry> entries;
            lock (HeavyProfiler.Entries)
                entries = orderByTime ? HeavyProfiler.Entries.OrderBy(a => a.BeforeStart).ToList() : HeavyProfiler.Entries.ToList();

            return View(ProfilerClient.ViewPrefix.Formato("HeavyList"), entries);
        }

        public ActionResult Statistics(SqlProfileResumeOrder order)
        {
            ProfilerPermission.ViewHeavyProfiler.Authorize();

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
            ProfilerPermission.ViewHeavyProfiler.Authorize();

            var entry = HeavyProfiler.Find(indices);

            ViewData[ViewDataKeys.Title] = "Entry " + entry.FullIndex(); 

            return View(ProfilerClient.ViewPrefix.Formato("HeavyDetails"), entry);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Enable()
        {
            ProfilerPermission.ViewHeavyProfiler.Authorize();

            Signum.Utilities.HeavyProfiler.Enabled = true;
            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Disable()
        {
            ProfilerPermission.ViewHeavyProfiler.Authorize();

            Signum.Utilities.HeavyProfiler.Enabled = false;
            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Clean()
        {
            ProfilerPermission.ViewHeavyProfiler.Authorize();

            Signum.Utilities.HeavyProfiler.Clean();
            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult UploadFile()
        {
            HttpPostedFileBase hpf = Request.Files[Request.Files.Cast<string>().Single()];

            using (StreamReader sr = new StreamReader(hpf.InputStream))
            {
                var doc = XDocument.Load(sr);
                HeavyProfiler.ImportXml(doc, true);
            }

            return RedirectToAction("Heavy");
        }

        public FileResult DownloadFile(string indices)
        {
            XDocument doc = indices == null ?
                HeavyProfiler.ExportXml() :
                HeavyProfiler.Find(indices).ExportXmlDocument();

            using (MemoryStream ms = new MemoryStream())
            {
                HeavyProfiler.ExportXml().Save(ms);

                string fileName = "Profile-{0}.xml".Formato(DateTime.Now.ToString("o").Replace(":", "."));

                //Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName); 

                return File(ms.ToArray(), "text/xml", fileName);
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Times()
        {
            ProfilerPermission.ViewTimeTracker.Authorize();

            return View(ProfilerClient.ViewPrefix.Formato("Times"));
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ClearTimes()
        {
            ProfilerPermission.ViewTimeTracker.Authorize();

            Signum.Utilities.TimeTracker.IdentifiedElapseds.Clear();
            return RedirectToAction("Times"); 
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult TimeTable()
        {
            ProfilerPermission.ViewTimeTracker.Authorize();

            return View(ProfilerClient.ViewPrefix.Formato("TimeTable"));
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ClearTimesTable()
        {
            ProfilerPermission.ViewTimeTracker.Authorize();

            Signum.Utilities.TimeTracker.IdentifiedElapseds.Clear();
            return RedirectToAction("TimeTable");
        }


        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult OverrideSessionTimeout(int? sec)
        {
            ProfilerPermission.OverrideSessionTimeout.Authorize();

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
