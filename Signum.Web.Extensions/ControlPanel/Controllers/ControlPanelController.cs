using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities.ControlPanel;
using Signum.Entities.Authorization;
using Signum.Entities;

namespace Signum.Web.ControlPanel
{
    [HandleException, AuthenticationRequired]
    public class ControlPanelController : Controller
    {
        
    }
}
