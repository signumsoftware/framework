using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities.Omnibox;
using Signum.React.Filters;

namespace Signum.React.Omnibox
{
    [ValidateModelFilter]
    public class OmniboxController : ControllerBase
    {
        [HttpPost("api/omnibox")]
        public List<OmniboxResult> OmniboxResults([Required, FromBody]OmniboxRequest request)
        {
            OmniboxPermission.ViewOmnibox.AssertAuthorized();

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
