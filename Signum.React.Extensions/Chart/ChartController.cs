using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Basics;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.ComponentModel.DataAnnotations;

namespace Signum.React.Chart
{
    public class ChartController : ControllerBase
    {
        [HttpGet("api/chart/scripts")]
        public List<ChartScript> ChartScripts()
        {
            return ChartScriptLogic.Scripts.Values.ToList();
        }
    }
}
