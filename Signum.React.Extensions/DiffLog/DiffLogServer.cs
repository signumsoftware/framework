using System.Reflection;
using Signum.React.Facades;
using Signum.Entities.DiffLog;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.DiffLog
{
    public static class DiffLogServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            ReflectionServer.RegisterLike(typeof(DiffLogMessage));
        }
    }
}