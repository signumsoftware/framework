using Signum.DiffLog;
using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.Authorization;
using Signum.Authorization.Rules;

namespace Signum.DiffLog;

public static class DiffLogServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        ReflectionServer.RegisterLike(typeof(DiffLogMessage), () => TypeAuthLogic.GetAllowed(typeof(OperationLogEntity)).MaxUI() > TypeAllowedBasic.None);
    }
}
