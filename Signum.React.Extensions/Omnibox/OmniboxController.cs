using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Entities.Omnibox;

namespace Signum.React.Omnibox
{
    public class OmniboxController : ApiController
    {
        [Route("api/omnibox"), HttpGet]
        public List<OmniboxResult> OmniboxResults(string omniboxQuery)
        {
            return OmniboxParser.Results(omniboxQuery, new System.Threading.CancellationToken());
        }
    }
}