using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.Map;

namespace Signum.Cache;

public static class CacheServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ReflectionServer.RegisterLike(typeof(CachePermission), () => CachePermission.ViewCache.IsAuthorized());

        MapColorProvider.GetColorProviders += GetMapColors;
    }

    static MapColorProvider[] GetMapColors()
    {
        if (!CachePermission.ViewCache.IsAuthorized())
            return new MapColorProvider[0];

        var groups = CacheLogic.Statistics().SelectMany(a => a.Explore()).GroupToDictionary(a => a.Table.Name.ToString());
        var semi = CacheLogic.SemiControllers.ToHashSet();

        var s = Schema.Current;

        var semiNames = semi.Select(t => s.Table(t).Name.ToString()).ToArray();

        return new[]
        {
            new MapColorProvider
            {
                Name = "cache-rows",
                NiceName = "Cache - Rows",
                AddExtra = t =>
                {
                    if (groups.ContainsKey(t.tableName))
                    {
                        var isSemi = semiNames.Contains(t.tableName);
                        t.extra["cache-semi"] = isSemi;
                        foreach (var mt in t.mlistTables)
                            if (groups.ContainsKey(mt.tableName))
                                mt.extra["cache-semi"] = isSemi;

                        t.extra["cache-rows"] = groups[t.tableName].Sum(a => a.Count);
                        foreach (var mt in t.mlistTables)
                            if (groups.ContainsKey(mt.tableName))
                                mt.extra["cache-rows"] = groups[mt.tableName].Sum(a => a.Count);
                    }
                },
                Order = 5
            },

            new MapColorProvider
            {
                Name = "cache-invalidations",
                NiceName = "Cache - Invalidations",
                AddExtra = t =>
                {
                    if (groups.ContainsKey(t.tableName))
                    {
                        t.extra["cache-invalidations"] = groups[t.tableName].Sum(a => a.Invalidations);
                        foreach (var mt in t.mlistTables)
                            if (groups.ContainsKey(mt.tableName))
                                mt.extra["cache-invalidations"] = groups[mt.tableName].Sum(a => a.Invalidations);
                    }
                },
                Order = 5,
            },

            new MapColorProvider
            {
                Name = "cache-loads",
                NiceName = "Cache - Loads",
                AddExtra = t =>
                {
                    if (groups.ContainsKey(t.tableName))
                    {
                        t.extra["cache-loads"] = groups[t.tableName].Sum(a => a.Loads);
                        foreach (var mt in t.mlistTables)
                            if (groups.ContainsKey(mt.tableName))
                                mt.extra["cache-loads"] = groups[mt.tableName].Sum(a => a.Loads);
                    }
                },
                Order = 5,
            },

            new MapColorProvider
            {
                Name = "cache-load-time",
                NiceName = "Cache - Load Time",
                AddExtra = t =>
                {
                    if (groups.ContainsKey(t.tableName))
                    {
                        t.extra["cache-load-time"] = groups[t.tableName].Sum(a => a.SumLoadTime.Milliseconds);
                        foreach (var mt in t.mlistTables)
                            if (groups.ContainsKey(mt.tableName))
                                mt.extra["cache-load-time"] = groups[mt.tableName].Sum(a => a.SumLoadTime.Milliseconds);
                    }
                },
                Order = 5,
            },
        };
    }


    static IEnumerable<CachedTableBase> Explore(this CachedTableBase root)
    {
        yield return root;

        if (root.SubTables != null)
        {
            foreach (var tab in root.SubTables)
            {
                foreach (var tab2 in tab.Explore()) //Quadratic but tipically very small
                {
                    yield return tab2;
                }
            }
        }
    }

}
