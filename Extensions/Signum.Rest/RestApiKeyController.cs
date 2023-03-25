using Microsoft.AspNetCore.Mvc;
using Signum.Authorization;

namespace Signum.Rest;

public class RestApiKeyController : ControllerBase
{
    [HttpGet("api/restApiKey/generate")]
    public string GenerateRestApiKey()
    {
        return RestApiKeyLogic.GenerateRestApiKey();
    }

    [HttpGet("api/restApiKey/current")]
    public string? GetAPIKey()
    {
        using (ExecutionMode.Global())
            return Database.Query<RestApiKeyEntity>().Where(a => a.User.Is(UserEntity.Current)).Select(a => a.ApiKey).SingleOrDefault();
    }
}
