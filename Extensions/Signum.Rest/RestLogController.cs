using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace Signum.Rest;

public class RestLogController : ControllerBase
{
    [HttpGet("api/restLog/")]
    public async Task<string> GetRestDiffLog(string id, string url)
    {
        var oldRequest = Database.Retrieve<RestLogEntity>(PrimaryKey.Parse(id, typeof(RestLogEntity)));
        if (!oldRequest.AllowReplay)
        {
            throw new InvalidOperationException("Replay not allowed for this RestLog");
        }
        var oldCredentials = Database.Query<RestApiKeyEntity>().Single(r => r.User.Is(oldRequest.User));

        var result = await RestLogLogic.GetRestDiffResult(new HttpMethod(oldRequest.HttpMethod!), url, oldCredentials.ApiKey, oldRequest.RequestBody.Text);

        return result;
    }
}
