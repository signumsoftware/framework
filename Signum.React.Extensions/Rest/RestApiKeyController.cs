using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine;
using Signum.Engine.Rest;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Rest;
using Signum.React.ApiControllers;

namespace Signum.React.Profiler
{
    public class RestApiKeyController : ApiController
    {
        [HttpGet("api/restApiKey/generate")]
        public string GenerateRestApiKey()
        {
            return RestApiKeyLogic.GenerateRestApiKey();
        }

        [HttpGet("api/restApiKey/current")]
        public string GetAPIKey()
        {
            using (ExecutionMode.Global())
                return Database.Query<RestApiKeyEntity>().Where(a => a.User.Is(UserEntity.Current)).Select(a => a.ApiKey).SingleOrDefault();
        }
    }
}