using Signum.Entities.Workflow;
using Signum.Engine.Workflow;
using Signum.React.Facades;
using System.Threading;
using Signum.Entities.Basics;
using Signum.Engine.Authorization;
using Signum.React.ApiControllers;
using Microsoft.AspNetCore.Mvc;
using Signum.React.Filters;
using static Signum.React.ApiControllers.OperationController;
using Signum.Entities.Reflection;
using System.ComponentModel.DataAnnotations;
using Signum.Entities.Isolation;
using Signum.Engine.Isolation;
using Signum.Entities.Authorization;

namespace Signum.React.Workflow;

[ValidateModelFilter]
public class IsolationController : Controller
{
    [HttpGet("api/isolations")]
    public List<Lite<IsolationEntity>> Isolations()
    {
        var current = UserEntity.Current.TryMixin<IsolationMixin>()?.Isolation;

        if (current != null)
            throw new UnauthorizedAccessException("User is only allowed to see isolation:" + current);

        return IsolationLogic.Isolations.Value;
    }
}
