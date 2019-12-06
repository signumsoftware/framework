using Signum.Engine.Basics;
using Signum.Engine.Dynamic;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.Dynamic
{
    public class DynamicClientController : ControllerBase
    {
        [HttpGet("api/dynamic/clients")]
        public List<DynamicClientEntity> GetClients()
        {
            var res = DynamicClientLogic.Clients.Value;
            return res;
        }
    }
}
