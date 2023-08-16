using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Chart.UserChart;
using Signum.UserAssets;

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
    public IEnumerable<UserAssetModel<UserChartEntity>> FromEntityType(string typeName)
    {
        return UserChartLogic.GetUserChartsModel(TypeLogic.GetType(typeName));
    }
}
