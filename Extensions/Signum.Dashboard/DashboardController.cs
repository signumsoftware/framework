using Microsoft.AspNetCore.Mvc;
using Signum.Authorization;
using Signum.UserAssets;

namespace Signum.Dashboard;

public class DashboardController : ControllerBase
{
    [HttpGet("api/dashboard/forEntityType/{typeName}")]
    public IEnumerable<Lite<DashboardEntity>> FromEntityType(string typeName)
    {
        return DashboardLogic.GetDashboardsModel(TypeLogic.GetType(typeName));
    }

    [HttpGet("api/dashboard/home")]
    public Lite<DashboardEntity>? Home()
    {
        var result = DashboardLogic.GetHomePageDashboard();
        return result?.ToLite();
    }

    static Lock lockKey = new ();

    [HttpPost("api/dashboard/get")]
    public DashboardWithCachedQueries GetDashboard([FromBody]Lite<DashboardEntity> dashboard)
    {
        var db = DashboardLogic.RetrieveDashboard(dashboard);

        if (db.CacheQueryConfiguration == null)
            return new DashboardWithCachedQueries { Dashboard = db, CachedQueries = new List<CachedQueryEntity>() };

        var cachedQueries = DashboardLogic.GetCachedQueries(dashboard).ToList();

        if (db.CacheQueryConfiguration.AutoRegenerateWhenOlderThan is int limit)
        {
            var maxDate = cachedQueries.Max(a => (DateTime?)a.CreationDate);

            if(maxDate == null  || maxDate.Value.AddMinutes(limit) < Clock.Now)
            {
                lock (lockKey)
                {
                    cachedQueries = DashboardLogic.GetCachedQueries(dashboard).ToList();
                    maxDate = cachedQueries.Max(a => (DateTime?)a.CreationDate);
                    if (maxDate == null || maxDate.Value.AddMinutes(limit) < Clock.Now)
                    {
                        using (UserHolder.UserSession(AuthLogic.SystemUser!))
                            db.Execute(DashboardOperation.RegenerateCachedQueries);

                        cachedQueries = DashboardLogic.GetCachedQueries(dashboard).ToList();
                    }
                }
            }
        }

        return new DashboardWithCachedQueries
        {
            Dashboard = db,
            CachedQueries =  cachedQueries,
        };
    }
}

public class DashboardWithCachedQueries
{
    public DashboardEntity Dashboard;
    public List<CachedQueryEntity> CachedQueries;
}
