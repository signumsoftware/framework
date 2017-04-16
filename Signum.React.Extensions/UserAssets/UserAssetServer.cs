using Signum.Entities.UserAssets;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Signum.React.ApiControllers;
using Signum.React.Facades;

namespace Signum.React.UserAssets
{
    public static class UserAssetServer
    {
        static bool started;
        public static void Start(HttpConfiguration config)
        {
            if (started)
                return;

            started = true;

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            ReflectionServer.RegisterLike(typeof(QueryTokenEmbedded));


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