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

            var firstColumnParts = cp.Parts.Where(p => p.Column == 1).ToList();
            var higherRowFirstColumn = firstColumnParts.Any() ? firstColumnParts.Max(p => p.Row) : 0;

            var newPart = new PanelPart
            {
                Row = higherRowFirstColumn + 1,
                Column = 1,
                Title = "",
                Content = (IIdentifiable)Activator.CreateInstance(Navigator.ResolveType(partType))
            };

            cp.Parts.Add(newPart);

            return Navigator.NormalPage(this, cp);
        }
    }
}
