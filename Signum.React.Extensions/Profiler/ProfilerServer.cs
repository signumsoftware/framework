using System.Reflection;
using Signum.React.Facades;
using Signum.Entities.Profiler;
using Microsoft.AspNetCore.Builder;
using Signum.React.Authorization;
using Signum.Engine.Authorization;

namespace Signum.React.Profiler
{
    public static class ProfilerServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            ReflectionServer.RegisterLike(typeof(ProfilerPermission), () => ProfilerPermission.ViewHeavyProfiler.IsAuthorized() || ProfilerPermission.ViewTimeTracker.IsAuthorized());
        }
    }
}
