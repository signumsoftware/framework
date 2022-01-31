using Microsoft.AspNetCore.Mvc;
using Signum.Entities.Toolbar;
using Signum.Engine.Toolbar;

namespace Signum.React.Toolbar;

public class ToolbarController : ControllerBase
{
    [HttpGet("api/toolbar/current")]
    public ToolbarResponse? Current()
    {
        return ToolbarLogic.GetCurrentToolbarResponse();
    }
}

