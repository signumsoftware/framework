using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.React.ApiControllers;
using Signum.Engine.Basics;
using Signum.Entities.UserAssets;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using Signum.React.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.Chart
{
    public class UserChartController : ApiController
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

        [HttpPost("api/userChart/fromChartRequest"), ValidateModelFilter]
        public UserChartEntity FromQueryRequest([FromBody]ChartRequest request)
        {
            return request.ToUserChart();
        }
    }
}