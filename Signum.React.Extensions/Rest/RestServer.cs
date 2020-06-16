using Signum.Engine.Rest;
using Signum.React.Filters;
using Signum.Engine;
using Signum.Utilities;
using System.Linq;
using System.Security.Authentication;
using Signum.Engine.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Builder;
using System.Reflection;

namespace Signum.React.Rest
{
    public static class RestServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            SignumAuthenticationFilter.Authenticators.Insert(0, ApiKeyAuthenticator);
        }

        private static SignumAuthenticationResult? ApiKeyAuthenticator(FilterContext ctx)
        {
            ctx.HttpContext.Request.Query.TryGetValue(RestApiKeyLogic.ApiKeyQueryParameter, out var val);
            ctx.HttpContext.Request.Headers.TryGetValue(RestApiKeyLogic.ApiKeyHeaderParameter, out var headerKeys);

            var keys = val.Distinct().Union(headerKeys.Distinct()).ToList();

            if (keys.Count() == 1)
            {
                using (AuthLogic.Disable())
                {
                    var user = RestApiKeyLogic.RestApiKeyCache.Value.GetOrThrow(keys.Single(), $"Could not authenticate with the API Key {keys.Single()}.").User.RetrieveAndRemember();
                    return new SignumAuthenticationResult { User = user };
                }
            }
            else if (keys.Count() > 1)
            {
                throw new AuthenticationException("Request contains multiple API Keys. Please use a single API Key per request for authentication.");
            }

            return null;
        }
    }
}
