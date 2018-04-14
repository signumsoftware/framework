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
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.React.UserAssets;
using Signum.Entities;
using Signum.React.ApiControllers;
using Signum.Entities.DynamicQuery;
using Signum.React.Maps;
using Signum.React.Facades;
using Signum.Engine.Cache;
using Signum.Entities.Cache;
using Signum.Engine.Authorization;
using Signum.Engine.Maps;
using Signum.Entities.Files;

namespace Signum.React.Files
{
    public static class FilesServer
    {
        public static void Start(HttpConfiguration config)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            PropertyConverter.GetPropertyConverters(typeof(FilePathEmbedded)).Add("fullWebPath", new PropertyConverter()
            {
                CustomWriteJsonProperty = ctx =>
                {
                    var csp = (FilePathEmbedded)ctx.Entity;
                    
                    ctx.JsonWriter.WritePropertyName(ctx.LowerCaseName);
                    ctx.JsonSerializer.Serialize(ctx.JsonWriter, csp.FullWebPath());
                },
                AvoidValidate = true,
                CustomReadJsonProperty = ctx =>
                {
                    var list = ctx.JsonSerializer.Deserialize(ctx.JsonReader);
                    //Discard
                }
            });
        }
    }
}