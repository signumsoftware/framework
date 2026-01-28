using Signum.UserAssets;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
using Signum.API;
using Signum.Authorization;
using Signum.API.Json;
using Signum.API.Controllers;
using Signum.Authorization.Rules;
using Signum.UserAssets.QueryTokens;
using Signum.UserAssets.Queries;

namespace Signum.UserAssets;

public static class UserAssetServer
{

    //EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(PinnedQueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx().Pinned));
    //EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(QueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx()));

    public static HashSet<PermissionSymbol> QueryPermissionSymbols = new HashSet<PermissionSymbol>();

    static bool started;
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        if (started)
            return;

        started = true;

        ReflectionServer.RegisterLike(typeof(UserAssetMessage), () => UserAssetsImporter.UserAssetNames.Values.Any(t => Schema.Current.IsAllowed(t, true) == null));
        ReflectionServer.RegisterLike(typeof(QueryOrderEmbedded), () => QueryPermissionSymbols.Any(p => p.IsAuthorized()));


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
