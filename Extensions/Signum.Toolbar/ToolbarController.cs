using Microsoft.AspNetCore.Mvc;

namespace Signum.Toolbar;

public class ToolbarController : ControllerBase
{
    [HttpGet("api/toolbar/current/{location}")]
    public ToolbarResponse? Current(ToolbarLocation location)
    {
        return ToolbarLogic.GetCurrentToolbarResponse(location);
    }
}

