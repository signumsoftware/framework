using System.ComponentModel;

namespace Signum.Caching;

[AllowUnauthenticated]
public enum CacheMessage
{
    Loading,
    CacheStatistics,
    Disable,
    Enable,
    Clear,
    ServerBroadcast,
    SqlDependency,
    Tables,
    Lazies,
    InvalidationExceptions,
    LazyStats,
    Type,
    Hits,
    Invalidations,
    Loads,
    LoadTime,
    NotLoaded,
    TableStats,
    Table,
    Count,
}
