using Signum.Entities.UserAssets;
using Signum.React.Json;
using Signum.Utilities;
using System.Reflection;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Microsoft.AspNetCore.Builder;
using Signum.Entities.UserQueries;
using Signum.Entities;

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
            ReflectionServer.RegisterLike(typeof(QueryTokenEmbedded));
            EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(QueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx()));
            EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(PinnedQueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx().Pinned));

            var pcs = PropertyConverter.GetPropertyConverters(typeof(QueryTokenEmbedded));
            pcs.Add("token", new PropertyConverter()
            {
                CustomWriteJsonProperty = ctx =>
                {
                    var qte = (QueryTokenEmbedded)ctx.Entity;

                    ctx.JsonWriter.WritePropertyName(ctx.LowerCaseName);
                    ctx.JsonSerializer.Serialize(ctx.JsonWriter, qte.TryToken == null ? null : new QueryTokenTS(qte.TryToken, true));
                },
                AvoidValidate = true,
                CustomReadJsonProperty = ctx =>
                {
                    var result = ctx.JsonSerializer.Deserialize(ctx.JsonReader);
                    //Discard
                }
            });
            pcs.Add("parseException", new PropertyConverter()
            {
                CustomWriteJsonProperty = ctx =>
                {
                    var qte = (QueryTokenEmbedded)ctx.Entity;

                    ctx.JsonWriter.WritePropertyName(ctx.LowerCaseName);
                    ctx.JsonSerializer.Serialize(ctx.JsonWriter, qte.ParseException?.Message);
                },
                AvoidValidate = true,
                CustomReadJsonProperty = ctx =>
                {
                    var result = ctx.JsonSerializer.Deserialize(ctx.JsonReader);
                    //Discard
                }
            });
            pcs.GetOrThrow("tokenString").CustomWriteJsonProperty = ctx =>
            {
                var qte = (QueryTokenEmbedded)ctx.Entity;

                ctx.JsonWriter.WritePropertyName(ctx.LowerCaseName);
                ctx.JsonWriter.WriteValue(qte.TryToken?.FullKey() ?? qte.TokenString);
            };
        }
    }
}
