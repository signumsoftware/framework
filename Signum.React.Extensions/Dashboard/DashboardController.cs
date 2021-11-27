using Microsoft.AspNetCore.Mvc;
using Signum.Entities.Dashboard;
using Signum.Engine.Dashboard;

namespace Signum.React.Dashboard;

public class DashboardController : ControllerBase
{
    [HttpGet("api/dashboard/forEntityType/{typeName}")]
    public IEnumerable<Lite<DashboardEntity>> FromEntityType(string typeName)
    {
        return DashboardLogic.GetDashboardsEntity(TypeLogic.GetType(typeName));
    }

    [HttpGet("api/dashboard/home")]
    public Lite<DashboardEntity>? Home()
    {
        var result = DashboardLogic.GetHomePageDashboard();
        return result?.ToLite();
    }

    [HttpPost("api/dashboard/get")]
    public DashboardWithCachedQueries GetDashboard([FromBody]Lite<DashboardEntity> dashboard)
    {
        return new DashboardWithCachedQueries
        {
            Dashboard = DashboardLogic.RetrieveDashboard(dashboard),
            CachedQueries = DashboardLogic.GetCachedQueries(dashboard).ToList(),
        };
    }
}

public class DashboardWithCachedQueries
{
    public DashboardEntity Dashboard;
    public List<CachedQueryEntity> CachedQueries;
}
