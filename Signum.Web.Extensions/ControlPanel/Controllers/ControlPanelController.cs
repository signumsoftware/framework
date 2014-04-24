using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities.ControlPanel;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Engine;
using Signum.Entities.Reflection;
using Signum.Entities.UserQueries;
using Signum.Engine.Authorization;
using Signum.Web.Operations;
using Signum.Engine.Basics;

namespace Signum.Web.ControlPanel
{
    public class ControlPanelController : Controller
    {
        public ViewResult View(Lite<ControlPanelDN> panel, Lite<IdentifiableEntity> currentEntity)
        {
            ControlPanelPermission.ViewControlPanel.Authorize();

            var cp = panel.Retrieve();

            if (cp.EntityType != null)
            {
                var filters = GraphExplorer.FromRoot(cp).OfType<QueryFilterDN>();
                var entity = currentEntity.Retrieve();
                CurrentEntityConverter.SetFilterValues(filters, entity);
            }

            return View(ControlPanelClient.ViewPrefix.Formato("ControlPanel"), cp);
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
