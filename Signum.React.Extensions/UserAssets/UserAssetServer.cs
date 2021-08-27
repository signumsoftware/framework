using Signum.Entities.UserAssets;
using Signum.React.Json;
using Signum.Utilities;
using System.Reflection;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Microsoft.AspNetCore.Builder;
using Signum.Entities.UserQueries;
using Signum.Entities;
using Signum.React.Authorization;
using Signum.Engine.Authorization;
using Signum.Entities.Chart;
using System.Text.Json;
using DocumentFormat.OpenXml.Bibliography;
using Signum.Engine.Json;

namespace Signum.React.UserAssets
{
    public static class UserAssetServer
    {
        static bool started;
        public static void Start(IApplicationBuilder app)
        {
            if (started)
                return;

            started = true;

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            ReflectionServer.RegisterLike(typeof(QueryTokenEmbedded), () => UserAssetPermission.UserAssetsToXML.IsAuthorized() ||
            TypeAuthLogic.GetAllowed(typeof(UserQueryEntity)).MaxUI() > Entities.Authorization.TypeAllowedBasic.None ||
            TypeAuthLogic.GetAllowed(typeof(UserChartEntity)).MaxUI() > Entities.Authorization.TypeAllowedBasic.None
            );
            //EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(QueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx()));
            //EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(PinnedQueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx().Pinned));

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
}
