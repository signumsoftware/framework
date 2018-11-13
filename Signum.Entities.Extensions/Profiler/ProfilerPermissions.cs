using Signum.Entities.Authorization;

namespace Signum.Entities.Profiler
{
    [AutoInit]
    public static class ProfilerPermission
    {
        public static PermissionSymbol ViewTimeTracker;
        public static PermissionSymbol ViewHeavyProfiler;
        public static PermissionSymbol OverrideSessionTimeout;
    }
}
