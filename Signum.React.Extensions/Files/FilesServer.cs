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
using Signum.Engine;
using Signum.Engine.Files;

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

            var s = Schema.Current.Settings;
            ReflectionServer.AddPropertyRouteExtension += (mi, pr) =>
            {
                var dft = s.FieldAttributes(pr)?.OfType<DefaultFileTypeAttribute>().SingleOrDefaultEx();
                if (dft != null)
                {
                    if (dft.FileTypeSymbol == null)
                    {
                        dft.FileTypeSymbol = SymbolLogic<FileTypeSymbol>.Symbols
                        .Where(a => a.Key.After(".") == dft.SymbolName && (dft.SymbolContainer == null || dft.SymbolContainer == a.Key.Before(".")))
                        .SingleEx(
                            () => $"No FileTypeSymbol with name {dft.SymbolName} is registered",
                            () => $"More than one FileTypeSymbol with name {dft.SymbolName} are registered. Consider desambiguating using symbolContainer argument in {pr}"
                        );
                    }

                    var alg = FileTypeLogic.GetAlgorithm(dft.FileTypeSymbol);

                    mi.Extension.Add("defaultFileTypeInfo", new
                    {
                        key = dft.FileTypeSymbol.Key,
                        onlyImages = alg.OnlyImages,
                        maxSizeInBytes = alg.MaxSizeInBytes,
                    });
                }
            };
        }
    }
}