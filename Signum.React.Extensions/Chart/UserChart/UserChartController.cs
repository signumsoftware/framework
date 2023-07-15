using Signum.Entities.Chart;
using Signum.Engine.Chart;
using Signum.React.Filters;
using Microsoft.AspNetCore.Mvc;
using Signum.Entities.UserAssets;

namespace Signum.React.Chart;

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
