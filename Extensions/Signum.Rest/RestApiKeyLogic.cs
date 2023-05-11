using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Signum.Rest;

public class RestApiKeyLogic
{
    public readonly static string ApiKeyQueryParameter = "apiKey";
    public readonly static string ApiKeyHeader = "X-ApiKey";

    public static ResetLazy<Dictionary<string, RestApiKeyEntity>> RestApiKeyCache = null!;
    public static Func<string> GenerateRestApiKey = () => DefaultGenerateRestApiKey();

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Include<RestApiKeyEntity>()
                .WithSave(RestApiKeyOperation.Save)
                .WithDelete(RestApiKeyOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.User,
                    e.ApiKey
                });

            RestApiKeyCache = sb.GlobalLazy(() =>
            {
                return Database.Query<RestApiKeyEntity>().ToDictionaryEx(rak => rak.ApiKey);
            }, new InvalidateWith(typeof(RestApiKeyEntity)));
        }
    }

    private static string DefaultGenerateRestApiKey()
    {
        byte[] tokenData = new byte[32];
        RandomNumberGenerator.Create().GetBytes(tokenData);
        return WebEncoders.Base64UrlEncode(tokenData);
    }
}
