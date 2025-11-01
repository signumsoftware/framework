using System.Security.Authentication;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Builder;
using Signum.API.Filters;
using Signum.API;
using Signum.Rest;
using Signum.Authorization;

namespace Signum.Rest;

public static class RestApiKeyServer
{
    public static void Start(WebServerBuilder app)
    {
        if (app.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        SignumAuthenticationFilter.Authenticators.Insert(0, ApiKeyAuthenticator);
    }

    private static SignumAuthenticationResult? ApiKeyAuthenticator(FilterContext ctx)
    {
        ctx.HttpContext.Request.Query.TryGetValue(RestApiKeyLogic.ApiKeyQueryParameter, out var val);
        ctx.HttpContext.Request.Headers.TryGetValue(RestApiKeyLogic.ApiKeyHeader, out var headerKeys);

        var keys = val.Distinct().Union(headerKeys.Distinct()).NotNull().ToList()!;

        if (keys.Count == 1)
        {
            using (AuthLogic.Disable())
            {
                var user = RestApiKeyLogic.RestApiKeyCache.Value.GetOrThrow(keys.Single(), $"Could not authenticate with the API Key {keys.Single()}.").User.RetrieveAndRemember();
                return new SignumAuthenticationResult { UserWithClaims = new UserWithClaims(user) };
            }
        }
        else if (keys.Count() > 1)
        {
            throw new AuthenticationException("Request contains multiple API Keys. Please use a single API Key per request for authentication.");
        }

        return null;
    }
}
