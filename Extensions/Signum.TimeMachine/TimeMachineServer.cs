using Signum.DiffLog;
using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.TimeMachine;

namespace Signum.DiffLog;

public static class TimeMachineServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        ReflectionServer.RegisterLike(typeof(TimeMachineMessage), () => TimeMachinePermission.ShowTimeMachine.IsAuthorized());
    }
}
