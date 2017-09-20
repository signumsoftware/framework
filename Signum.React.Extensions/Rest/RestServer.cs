using Signum.Engine.Rest;
using Signum.React.Filters;
using Signum.Engine;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Web.Http.Controllers;
using Signum.Engine.Authorization;
using Signum.Entities.Basics;

namespace Signum.React.Rest
{
    public static class RestServer
    {
        public static void Start()
        {
            SignumAuthenticationFilterAttribute.Authenticators.Insert(0, ApiKeyAuthenticator);
        }

        private static SignumAuthenticationResult ApiKeyAuthenticator(HttpActionContext ctx)
        {
            var nvp = ctx.Request.RequestUri.ParseQueryString();
            IEnumerable<string> queryKeys = ctx.Request.RequestUri
                .ParseQueryString()
                .GetValues(RestApiKeyLogic.ApiKeyQueryParameter)?
                .Distinct() ?? Enumerable.Empty<string>();

            IEnumerable<string> headerKeys;
            ctx.Request.Headers.TryGetValues(RestApiKeyLogic.ApiKeyHeaderParameter, out headerKeys);

            var keys = queryKeys.Union(headerKeys ?? Enumerable.Empty<string>());

            if (keys.Count() == 1)
            {
                using (AuthLogic.Disable())
                {
                    var user = RestApiKeyLogic.RestApiKeyCache.Value.GetOrThrow(keys.Single(), $"Could not authenticate with the API Key {keys.Single()}.").User.Retrieve();
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