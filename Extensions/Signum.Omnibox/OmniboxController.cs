using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Entities.Omnibox;

namespace Signum.React.Omnibox;

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
