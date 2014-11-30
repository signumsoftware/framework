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
        public ViewResult View(Lite<DashboardEntity> panel, Lite<Entity> currentEntity)
        {
            DashboardPermission.ViewDashboard.AssertAuthorized();

            var cp = DashboardLogic.RetrieveDashboard(panel);

           

            if (cp.EntityType != null)
            {
                if (currentEntity == null)
                    throw new ArgumentNullException("currentEntity");

                ViewData["currentEntity"] = currentEntity.Retrieve();
            }

            return View(DashboardClient.ViewPrefix.FormatWith("Dashboard"), cp);
        }

        public ActionResult AddNewPart(string rootType, string propertyRoute, string newPartType, string partialViewName)
        {
            var type = Navigator.ResolveType(newPartType);

            PanelPartEntity part = new PanelPartEntity
            {
                StartColumn = 0,
                Columns = 12,
                Content = (IPartEntity)Activator.CreateInstance(type),
            };

            PropertyRoute route = PropertyRoute.Parse(TypeLogic.GetType(rootType), propertyRoute);
            ViewData[GridRepeaterHelper.LastEnd] = 0;
            return Navigator.PartialView(this, new TypeContext<PanelPartEntity>(part, null, this.Prefix(), route), partialViewName);
        }
    }
}
