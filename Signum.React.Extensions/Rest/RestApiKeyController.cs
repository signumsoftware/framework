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
        [Route("api/restApiKey/generate"), HttpGet]
        public string GenerateRestApiKey()
        {
            return RestApiKeyLogic.GenerateRestApiKey();
        }

        [Route("api/restApiKey/current"), HttpGet]
        public string GetAPIKey()
        {
            using (ExecutionMode.Global())
                return Database.Query<RestApiKeyEntity>().Where(a => a.User.RefersTo(UserEntity.Current)).Select(a => a.ApiKey).SingleOrDefault();
        }
    }
}