using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Profiler;

public static class ProfilerServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ReflectionServer.RegisterLike(typeof(ProfilerPermission), () => ProfilerPermission.ViewHeavyProfiler.IsAuthorized() || ProfilerPermission.ViewTimeTracker.IsAuthorized());
    }
}
