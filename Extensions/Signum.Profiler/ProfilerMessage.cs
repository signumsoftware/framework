using System.ComponentModel;

namespace Signum.Profiler;

[AllowUnauthenticated]
public enum ProfilerMessage
{
    HeavyProfiler,
    [Description("Entry {0} (loading...)")]
    Entry0Loading,
    [Description("Entry {0}")]
    Entry0_,
    Role,
    Time,
    Download,
    Update,
    AdditionalData,
    [Description("StackTrace")]
    StackTrace,
    [Description("No StackTrace")]
    NoStackTrace,
    [Description("StackTrace Overview")]
    StackTraceOverview,
    AsyncStack,
    Namespace,
    Type,
    Method,
    FileLine,
}


[AllowUnauthenticated]
public enum TimeMessage
{
    [Description("Times (loading...)")]
    TimesLoading,
    Times,
    Reload,
    Clear,
    Bars,
    Table,
    Average,
    Executed,
    Total,
    NoDuration,
    TimesOverview,
    Name, 
    Entity,
    Count,
    Min,
    Max,
    Last,
    TimeStatistics,
}
