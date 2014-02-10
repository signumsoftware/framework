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

        public ActionResult AddNewPart()
        {
            var cp = this.ExtractEntity<ControlPanelDN>().ApplyChanges(this.ControllerContext, true).Value;

            var lastColumn = 0.To(cp.NumberOfColumns).WithMin(c => cp.Parts.Count(p => p.Column == c));

            var newPart = new PanelPartDN
            {
                Column = lastColumn,
                Row = (cp.Parts.Where(a => a.Column == lastColumn).Max(a => (int?)a.Row + 1) ?? 0),
                Title = "",
                Content = (IPartDN)Activator.CreateInstance(Navigator.ResolveType(Request["newPartType"]))
            };

            cp.Parts.Add(newPart);

            return OperationClient.DefaultExecuteResult(this, cp);
        }
    }
}
