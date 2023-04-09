using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Chart.UserChart;

namespace Signum.Chart;

[ValidateModelFilter]
public class UserChartController : ControllerBase
{
    [HttpGet("api/userChart/forQuery/{queryKey}")]
    public IEnumerable<Lite<UserChartEntity>> FromQuery(string queryKey)
    {
        return UserChartLogic.GetUserCharts(QueryLogic.ToQueryName(queryKey));
    }

    [HttpGet("api/userChart/forEntityType/{typeName}")]
    public IEnumerable<Lite<UserChartEntity>> FromEntityType(string typeName)
    {
        return UserChartLogic.GetUserChartsEntity(TypeLogic.GetType(typeName));
    }
}
