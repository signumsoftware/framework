using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities.Dashboard;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Engine;
using Signum.Entities.Reflection;
using Signum.Entities.UserQueries;
using Signum.Engine.Authorization;
using Signum.Web.Operations;
using Signum.Engine.Basics;
using Signum.Engine.Dashboard;

namespace Signum.Web.Dashboard
{
    public class DashboardController : Controller
    {
        public ViewResult View(Lite<DashboardDN> panel, Lite<IdentifiableEntity> currentEntity)
        {
            DashboardPermission.ViewDashboard.Authorize();

            var cp = DashboardLogic.RetrieveDashboard(panel);

           

            if (cp.EntityType != null)
            {
                if (currentEntity == null)
                    throw new ArgumentNullException("currentEntity");

                ViewData["currentEntity"] = currentEntity.Retrieve();
            }

            return View(DashboardClient.ViewPrefix.Formato("Dashboard"), cp);
        }

        public ActionResult AddNewPart(string rootType, string propertyRoute, string newPartType, string partialViewName)
        {
            var type = Navigator.ResolveType(newPartType);

            PanelPartDN part = new PanelPartDN
            {
                StartColumn = 0,
                Columns = 12,
                Content = (IPartDN)Activator.CreateInstance(type),
            };

            PropertyRoute route = PropertyRoute.Parse(TypeLogic.GetType(rootType), propertyRoute);
            ViewData[GridRepeaterHelper.LastEnd] = 0;
            return Navigator.PartialView(this, new TypeContext<PanelPartDN>(part, null, this.Prefix(), route), partialViewName);
        }
    }
}
