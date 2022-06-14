using Microsoft.AspNetCore.Mvc;
using Signum.React.Filters;
using Signum.Entities.Isolation;
using Signum.Engine.Isolation;
using Signum.Entities.Authorization;

namespace Signum.React.Isolation;

[ValidateModelFilter]
public class IsolationController : Controller
{
    [HttpGet("api/isolations")]
    public List<Lite<IsolationEntity>> Isolations()
    {
        var current = UserEntity.Current.Retrieve().TryMixin<IsolationMixin>()?.Isolation;

        if (current != null)
            throw new UnauthorizedAccessException("User is only allowed to see isolation:" + current);

        return IsolationLogic.Isolations.Value;
    }
}
