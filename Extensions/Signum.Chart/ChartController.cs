using Microsoft.AspNetCore.Mvc;
using Signum.Entities.Chart;
using Signum.Engine.Chart;

namespace Signum.React.Chart;

public class ChartController : ControllerBase
{
    [HttpGet("api/chart/scripts")]
    public List<ChartScript> ChartScripts()
    {
        return ChartScriptLogic.Scripts.Values.ToList();
    }
}
