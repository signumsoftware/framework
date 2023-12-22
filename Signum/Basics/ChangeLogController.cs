using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;

namespace Signum.Basics;

[ValidateModelFilter]
public class ChangeLogController : ControllerBase
{
    [HttpGet("api/changelog/getLastDate")]
    public DateTime? GetLastDate()
    {
        return ChangeLogLogic.GetLastDate();
    }

        [HttpPost("api/changelog/updateLastDate")]
    public void UpdateLastDate()
    {
        ChangeLogLogic.UpdateLastDate();
    }
}
