using System.Web.Http;
using Signum.Engine.Rest;

namespace Signum.React.Profiler
{
    public class RestApiKeyController : ApiController
    {
        [Route("api/restApiKey"), HttpGet]
        public string GenerateRestApiKey()
        {
            return RestApiKeyLogic.GenerateRestApiKey();
        }
    }
}