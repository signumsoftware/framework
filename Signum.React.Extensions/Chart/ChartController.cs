using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
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
using System.Threading;
using System.Threading.Tasks;

namespace Signum.React.Chart
{
    public class ChartController : ApiController
    {
        [HttpGet("api/chart/scripts")]
        public List<List<ChartScriptEntity>> ChartScripts()
        {
            return ChartUtils.PackInGroups(ChartScriptLogic.Scripts.Value.Values, 4);
        }

        [HttpGet("api/chart/colorPalettes")]
        public List<string> ColorPelettes()
        {
            return ChartColorLogic.Colors.Value.Keys.Select(t => TypeLogic.GetCleanName(t)).ToList();
        }
    }
}
