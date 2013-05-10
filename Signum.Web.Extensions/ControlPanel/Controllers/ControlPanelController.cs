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

namespace Signum.Web.ControlPanel
{
    public class ControlPanelController : Controller
    {
        public ViewResult View(Lite<ControlPanelDN> panel)
        { 
            return View(ControlPanelClient.ViewPrefix.Formato("ControlPanel"), panel.Retrieve());
        }

        public ActionResult AddNewPart()
        {
            string partType = Request.Form["newPartType"];

            var cp = this.ExtractEntity<ControlPanelDN>().ApplyChanges(this.ControllerContext, "", true).Value;

            var lastColumn = 0.To(cp.NumberOfColumns).WithMin(c => cp.Parts.Count(p => p.Column == c));

            var newPart = new PanelPartDN
            {
                Column = lastColumn,
                Row = (cp.Parts.Where(a => a.Column == lastColumn).Max(a => (int?)a.Row + 1) ?? 0),
                Title = "",
                Content = (IPartDN)Activator.CreateInstance(Navigator.ResolveType(partType))
            };

            cp.Parts.Add(newPart);

            return Navigator.NormalPage(this, cp);
        }
    }
}
