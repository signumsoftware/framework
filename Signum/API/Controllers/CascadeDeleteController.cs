using Microsoft.AspNetCore.Mvc;

namespace Signum.API.Controllers;

[ApiController]
public class CascadeDeleteController : ControllerBase
{
    [HttpPost("api/cascadeDelete/references")]
    public List<CascadeReferenceDto> GetReferences([FromBody] Lite<Entity> lite)
    {
        return CascadeDeleteLogic.GetReferences(lite);
    }
}
