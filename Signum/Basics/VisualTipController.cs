using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Signum.API.Filters;

namespace Signum.Basics;

[ValidateModelFilter]
public class VisualTipController : ControllerBase
{
    [HttpGet("api/visualtip/getConsumed")]
    public List<string>? GetConsumed()
    {
        return VisualTipLogic.GetConsumed()?.Select(s => s.Key).ToList();
    }

    [HttpPost("api/visualtip/consume")]
    public void Consume([Required, FromBody] string symbolKey)
    {
        VisualTipLogic.Consume(symbolKey);
    }
}
