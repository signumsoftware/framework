using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Basics;
using Signum.Entities.Chart;
using Signum.Engine.Chart;

namespace Signum.React.Chart
{
    public class ChartController : ControllerBase
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
