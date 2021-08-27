using System.Reflection;
using Signum.React.Facades;
using Signum.Entities.DiffLog;
using Microsoft.AspNetCore.Builder;
using Signum.React.Authorization;
using Signum.Engine.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Authorization;

namespace Signum.React.DiffLog
{
    public static class DiffLogServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            ReflectionServer.RegisterLike(typeof(DiffLogMessage), () => TimeMachinePermission.ShowTimeMachine.IsAuthorized() || TypeAuthLogic.GetAllowed(typeof(OperationLogEntity)).MaxUI() >  TypeAllowedBasic.None);
        }
    }
}
