using Microsoft.AspNetCore.Mvc;

namespace Signum.Toolbar;

public class ToolbarController : ControllerBase
{
    [HttpGet("api/toolbar/current/{location}")]
    public ToolbarResponse? Current(ToolbarLocation location)
    {
        return ToolbarLogic.GetCurrentToolbarResponse(location);
    }

    [HttpGet("api/toolbarMenu/{menuId}")]
    public ToolbarResponse? ToolbarMenu(string menuId)
    { 
        var tm = Lite.ParsePrimaryKey<ToolbarMenuEntity>(menuId);

        return ToolbarLogic.GetToolbarMenuResponse(tm);
    }
}

