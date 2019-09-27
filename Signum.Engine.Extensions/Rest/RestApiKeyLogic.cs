using System;
using System.Collections.Generic;
using System.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities.Rest;
using Signum.Engine.Operations;
using Signum.Utilities;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Signum.Engine.Rest
{
    public class RestApiKeyLogic
    {
        public readonly static string ApiKeyQueryParameter = "apiKey";
        public readonly static string ApiKeyHeaderParameter = "X-ApiKey";

        public static ResetLazy<Dictionary<string, RestApiKeyEntity>> RestApiKeyCache = null!;
        public static Func<string> GenerateRestApiKey = () => DefaultGenerateRestApiKey();

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<RestApiKeyEntity>()
                    .WithDelete(RestApiKeyOperation.Delete)
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.User,
                        e.ApiKey
                    });

                new Graph<RestApiKeyEntity>.Execute(RestApiKeyOperation.Save)
                {
                    CanBeNew = true,
                    CanBeModified = true,
                    Execute = (e, _) => { },
                }.Register();

                RestApiKeyCache = sb.GlobalLazy(() =>
                {
                    return Database.Query<RestApiKeyEntity>().ToDictionaryEx(rak => rak.ApiKey);
                }, new InvalidateWith(typeof(RestApiKeyEntity)));
            }
        }

        private static string DefaultGenerateRestApiKey()
        {
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return WebEncoders.Base64UrlEncode(tokenData);
            }
        }
    }
}
