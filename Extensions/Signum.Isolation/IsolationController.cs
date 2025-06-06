using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;

namespace Signum.Isolation;

[ValidateModelFilter]
public class IsolationController : Controller
{
    [HttpGet("api/isolations")]
    public List<Lite<IsolationEntity>> Isolations()
    {
        var current = (IsolationMixin?)UserHolder.Current.GetClaim("Isolation");

        if (current != null)
            throw new UnauthorizedAccessException("User is only allowed to see isolation:" + current);

        return IsolationLogic.Isolations.Value;
    }
}
