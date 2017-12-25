using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Signum.Engine;
using Signum.Entities;
using Signum.Entities.Rest;
using Signum.Utilities;
using Signum.Engine.Rest;

namespace Signum.React.RestLog
{
    public class RestLogController : ApiController
    {
        [Route("api/restLog/"), HttpGet]
        public async Task<RestDiffResult> GetRestDiffLog(string id, string url)
        {
            var oldRequest = Database.Retrieve<RestLogEntity>(PrimaryKey.Parse(id, typeof(RestLogEntity)));
            if (!oldRequest.AllowReplay)
            {
                throw new InvalidOperationException("Replay not allowed for this RestLog");
            }
            var oldCredentials = Database.Query<RestApiKeyEntity>().Single(r => r.User.Is(oldRequest.User));

            var result = await RestLogLogic.GetRestDiffResult(url, oldCredentials.ApiKey, oldRequest.RequestBody, oldRequest.ResponseBody);

            return RestLogLogic.RestDiffLog(result);
        }

       
        [Route("api/restLog/"), HttpPost]
        public async Task<RestDiffResult> GetDiff(RestDiffRequest request)
        {
            var restDiffResult = await RestLogLogic.GetRestDiffResult(request.url, request.apiKey, request.requestBody, request.responseBody);
            

            return RestLogLogic.RestDiffLog(restDiffResult);
        }

       
    }

   
}