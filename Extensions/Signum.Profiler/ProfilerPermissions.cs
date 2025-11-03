using System.ComponentModel;

namespace Signum.Profiler;

[AutoInit]
public static class ProfilerPermission
{
    public static PermissionSymbol ViewTimeTracker;
    public static PermissionSymbol ViewHeavyProfiler;
    public static PermissionSymbol OverrideSessionTimeout;
}


public enum HeavyProfilerMessage
{
    [Description("Heavy Profiler (loading...)")]
    HeavyProfilerLoading,
    HeavyProfiler,
    Upload,
    Record,
    Update,
    Clear,
    Download,
    IgnoreHeavyProfilerEntries,
    [Description("Upload previous runs to compare performance.")]
    UploadPreviousRunsToComparePerformance,
    [Description("Enable the profiler with the debugger with {0} and save the results with {1}")]
    EnableTheProfilerWithTheDebuggerWith0AndSaveTheResultsWith1,
    Entries,
}
