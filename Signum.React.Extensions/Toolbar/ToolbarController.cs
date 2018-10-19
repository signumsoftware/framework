using Signum.Entities.Workflow;
using Signum.Engine.Workflow;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.React.Facades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using Signum.Entities.Toolbar;
using Signum.Engine.Toolbar;
using Signum.React.ApiControllers;

namespace Signum.React.Toolbar
{
    public class ToolbarController : ApiController
    {
        [HttpGet("api/toolbar/current/{location}")]
        public ToolbarResponse Current(ToolbarLocation location)
        {
            return ToolbarLogic.GetCurrentToolbarResponse(location);
        }
    }
}

