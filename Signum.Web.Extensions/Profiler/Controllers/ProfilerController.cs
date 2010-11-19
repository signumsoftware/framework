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

namespace Signum.Web.Profiler
{
    [HandleException, AuthenticationRequired]
    public class ProfilerController : Controller
    {
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ViewAll()
        {
            return View(ProfileClient.ViewPath + "ProfilerList", Signum.Utilities.Profiler.Entries); 
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ViewRoute(string indices)
        {
            int[] ind = indices.Split(',').Select(a => int.Parse(a)).ToArray(); 

            ProfilerEntryDetails details = new ProfilerEntryDetails
            {
                Indices = ind,
                Previous = new List<ProfilerEntry>(),
                Entry = null, 
            };

            List<ProfilerEntry> currentList = Signum.Utilities.Profiler.Entries;
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

            return View(ProfileClient.ViewPath + "ProfilerDetails", details);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Enable()
        {
            Signum.Utilities.Profiler.Enabled = true;
            return RedirectToAction("ViewAll");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Disable()
        {
            Signum.Utilities.Profiler.Enabled = false;
            return RedirectToAction("ViewAll");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Clean()
        {
            Signum.Utilities.Profiler.Clean();
            return RedirectToAction("ViewAll");
        }
    }

    public class ProfilerEntryDetails
    {
        public int[] Indices; 
        public ProfilerEntry Entry;
        public List<ProfilerEntry> Previous;  
    }
}
