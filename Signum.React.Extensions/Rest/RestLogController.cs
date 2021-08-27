using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine;
using Signum.Entities;
using Signum.Entities.Rest;
using Signum.Engine.Rest;

namespace Signum.React.RestLog
{
    public class RestLogController : ControllerBase
    {
        [HttpGet("api/restLog/")]
        public async Task<RestDiffResult> GetRestDiffLog(string id, string url)
        {
            var oldRequest = Database.Retrieve<RestLogEntity>(PrimaryKey.Parse(id, typeof(RestLogEntity)));
            if (!oldRequest.AllowReplay)
            {
                throw new InvalidOperationException("Replay not allowed for this RestLog");
            }
            var oldCredentials = Database.Query<RestApiKeyEntity>().Single(r => r.User.Is(oldRequest.User));

            var result = await RestLogLogic.GetRestDiffResult(new HttpMethod(oldRequest.HttpMethod!), url, oldCredentials.ApiKey, oldRequest.RequestBody, oldRequest.ResponseBody);

            return RestLogLogic.RestDiffLog(result);
        }
    }
}
