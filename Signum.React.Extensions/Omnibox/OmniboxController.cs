using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Entities.Omnibox;
using Signum.React.ApiControllers;

namespace Signum.React.Omnibox
{
    public class OmniboxController : ApiController
    {
        [HttpPost("api/omnibox")]
        public List<OmniboxResult> OmniboxResults([FromBody]OmniboxRequest request)
        {
            ReactSpecialOmniboxGenerator.ClientGenerator = new SpecialOmniboxGenerator<ReactSpecialOmniboxAction>()
            {
                Actions = request.specialActions.ToDictionary(a => a, a => new ReactSpecialOmniboxAction { Key = a })
            };

            return OmniboxParser.Results(request.query, new System.Threading.CancellationToken());
        }

        public class OmniboxRequest
        {
            public string query;
            public string[] specialActions;
        }
    }
}