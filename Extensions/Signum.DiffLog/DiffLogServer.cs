using Signum.DiffLog;
using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.Authorization;
using Signum.Authorization.Rules;

namespace Signum.DiffLog;

public static class DiffLogServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ReflectionServer.RegisterLike(typeof(DiffLogMessage), () => TypeAuthLogic.GetAllowed(typeof(OperationLogEntity)).MaxUI() > TypeAllowedBasic.None);
    }
}
