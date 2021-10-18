using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Signum.Entities;
using Signum.Engine.Basics;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using Signum.React.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.Chart
{
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
}
