using Microsoft.AspNetCore.Mvc;

namespace Signum.Dynamic.Client;

public class DynamicClientController : ControllerBase
{
    [HttpGet("api/dynamic/clients")]
    public List<DynamicClientEntity> GetClients()
    {
        var res = DynamicClientLogic.Clients.Value;
        return res;
    }
}
