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

namespace Signum.React.UserAssets
{
    public static class UserAssetsServer
    {
        static bool started;
        public static void Start(HttpConfiguration config)
        {
            if (started)
                return;

            started = true;

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            var pcs = PropertyConverter.GetPropertyConverters(typeof(QueryTokenEntity));
            pcs.Remove("token");
            pcs.GetOrThrow("tokenString").CustomWriteJsonProperty = ctx =>
            {
                var qte = (QueryTokenEntity)ctx.Entity;

                ctx.JsonWriter.WritePropertyName(ctx.LowerCaseName);
                ctx.JsonWriter.WriteValue(qte.Token?.FullKey() ?? qte.TokenString);
            };
        }
    }
}