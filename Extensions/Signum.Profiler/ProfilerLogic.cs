using Signum.API;
using Signum.Authorization.Rules;

namespace Signum.Profiler;

public static class ProfilerLogic
{
    static Variable<int?> SessionTimeoutVariable = Statics.SessionVariable<int?>("sessionTimeout");
    public static int? SessionTimeout
    {
        get { return SessionTimeoutVariable.Value; }
        set { SessionTimeoutVariable.Value = value; }
    }

    public static void Start(SchemaBuilder sb, bool timeTracker, bool heavyProfiler, bool overrideSessionTimeout)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            if (timeTracker)
                PermissionLogic.RegisterPermissions(ProfilerPermission.ViewTimeTracker);

            if (heavyProfiler)
                PermissionLogic.RegisterPermissions(ProfilerPermission.ViewHeavyProfiler);

            if (overrideSessionTimeout)
                PermissionLogic.RegisterPermissions(ProfilerPermission.OverrideSessionTimeout);

            if (sb.WebServerBuilder != null)
                ProfilerServer.Start(sb.WebServerBuilder);
        }
    }

    public static void ProfilerEntries(List<HeavyProfilerEntry> entries)
    {
        HeavyProfiler.ImportEntries(entries, rebaseTime: false);
    }
}
