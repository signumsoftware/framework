using Signum.UserAssets;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
using Signum.API;
using Signum.Authorization;
using Signum.API.Json;
using Signum.API.Controllers;
using Signum.Authorization.Rules;
using Signum.UserAssets.QueryTokens;

namespace Signum.UserAssets;

public static class UserAssetServer
{
    public static Func<bool> RegisterLikeUserAssets = () => UserAssetPermission.UserAssetsToXML.IsAuthorized();
        //EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(PinnedQueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx().Pinned));
        //EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(QueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx()));

    static bool started;
    public static void Start(IApplicationBuilder app)
    {
        if (started)
            return;

        started = true;

        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        ReflectionServer.RegisterLike(typeof(QueryTokenEmbedded), () => RegisterLikeUserAssets.GetInvocationListTyped().Any(a => a()));

        var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(QueryTokenEmbedded));
        pcs.Add("token", new PropertyConverter
        {
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var qte = (QueryTokenEmbedded)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                JsonSerializer.Serialize(writer, qte.TryToken == null ? null : new QueryTokenTS(qte.TryToken, true), ctx.JsonSerializerOptions);
            },
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                var result = JsonSerializer.Deserialize<object>(ref reader, ctx.JsonSerializerOptions);
                //Discard
            }
        });
        pcs.Add("parseException", new PropertyConverter
        {
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var qte = (QueryTokenEmbedded)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                writer.WriteStringValue(qte.ParseException?.Message);
            },
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                var result = reader.GetString();
                //Discard
            }
        });
        pcs.GetOrThrow("tokenString").CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
        {
            var qte = (QueryTokenEmbedded)ctx.Entity;

            writer.WritePropertyName(ctx.LowerCaseName);
            writer.WriteStringValue(qte.TryToken?.FullKey() ?? qte.TokenString);
        };
    }
}
