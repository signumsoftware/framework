using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Signum.Entities.Rest;
using Signum.Engine.Rest;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Signum.React.RestLog;

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

        var result = await RestLogLogic.GetRestDiffResult(new HttpMethod(oldRequest.HttpMethod!), url, oldCredentials.ApiKey, oldRequest.RequestBody);

        return result;
    }
}
