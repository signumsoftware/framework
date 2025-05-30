using Microsoft.AspNetCore.Mvc;
using Signum.Chart;

namespace Signum.Chart;

public class ChartController : ControllerBase
{
    [HttpGet("api/chart/scripts")]
    public List<ChartScript> ChartScripts()
    {
        return ChartScriptLogic.Scripts.Values.ToList();
    }
}
