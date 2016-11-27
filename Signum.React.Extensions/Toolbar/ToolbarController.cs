using Signum.Entities.Workflow;
using Signum.Logic.Workflow;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.React.Facades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using Signum.Entities.Toolbar;
using Signum.Engine.Toolbar;

namespace Signum.React.Toolbar
{
    public class ToolbarController : ApiController
    {
        [Route("api/toolbar/current"), HttpGet]
        public ToolbarResponse Current()
        {
            return ToolbarLogic.GetCurrentResponse();
        }
    }
}

