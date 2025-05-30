using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Profiler;

public static class ProfilerServer
{
    public static void Start(IApplicationBuilder app)
    {
        ReflectionServer.RegisterLike(typeof(ProfilerPermission), () => ProfilerPermission.ViewHeavyProfiler.IsAuthorized() || ProfilerPermission.ViewTimeTracker.IsAuthorized());
    }
}
