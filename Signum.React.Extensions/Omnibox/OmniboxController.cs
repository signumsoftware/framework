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

            var generator = new SpecialOmniboxGenerator<ReactSpecialOmniboxAction>()
            {
                Actions = request.specialActions.ToDictionary(a => a, a => new ReactSpecialOmniboxAction { Key = a })
            };

            using (ReactSpecialOmniboxGenerator.OverrideClientGenerator(generator))
            {
                return OmniboxParser.Results(request.query, new System.Threading.CancellationToken());
            }
        }

        public class OmniboxRequest
        {
            public string query;
            public string[] specialActions;
        }
    }
}
