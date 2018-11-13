using System.Reflection;
using Signum.React.Facades;
using Signum.Entities.Profiler;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.Profiler
{
    public static class ProfilerServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            ReflectionServer.RegisterLike(typeof(ProfilerPermission));
        }
    }
}