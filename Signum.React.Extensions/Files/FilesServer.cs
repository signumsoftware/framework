using Signum.React.Json;
using Signum.Utilities;
using System.Linq;
using System.Reflection;
using Signum.React.Facades;
using Signum.Engine.Maps;
using Microsoft.AspNetCore.Builder;
using Signum.Entities.Files;
using Signum.Engine;
using Signum.Engine.Files;
using Signum.React.Filters;
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Signum.Engine.Json;

namespace Signum.React.Files
{
    public static class FilesServer
    {
        public static Func<string, string> Content = (url) => url;

        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            void RegisterFilePathEmbeddedProperty(string propertyName, Func<FilePathEmbedded, object?> getter)
            {
                SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(FilePathEmbedded)).Add(propertyName, new PropertyConverter()
                {
                    CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
                    {
                        var fpe = (FilePathEmbedded)ctx.Entity;

                        writer.WritePropertyName(ctx.LowerCaseName);
                        JsonSerializer.Serialize(writer, getter(fpe), ctx.JsonSerializerOptions);
                    },
                    AvoidValidate = true,
                    CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
                    {
                        var obj = JsonSerializer.Deserialize<object>(ref reader);
                        //Discard
                    }
                });
            }

            RegisterFilePathEmbeddedProperty("fullWebPath", fpe => fpe.FullWebPath());
            RegisterFilePathEmbeddedProperty("entityId", fpe => fpe.EntityId.Object);
            RegisterFilePathEmbeddedProperty("mListRowId", fpe => fpe.MListRowId?.Object);
            RegisterFilePathEmbeddedProperty("propertyRoute", fpe => fpe.PropertyRoute);
            RegisterFilePathEmbeddedProperty("rootType", fpe => fpe.RootType);

            var s = Schema.Current.Settings;
            ReflectionServer.PropertyRouteExtension += (mi, pr) =>
            {
                var dft = s.FieldAttributes(pr)?.OfType<DefaultFileTypeAttribute>().SingleOrDefaultEx();
                if (dft != null)
                {
                    if (dft.FileTypeSymbol == null)
                    {
                        dft.FileTypeSymbol = SymbolLogic<FileTypeSymbol>.Symbols
                        .Where(a => a.Key.After(".") == dft.SymbolName && (dft.SymbolContainer == null || dft.SymbolContainer == a.Key.Before(".")))
                        .SingleEx(
                            () => $"No FileTypeSymbol with name {dft.SymbolContainer}.{dft.SymbolName} is registered",
                            () => $"More than one FileTypeSymbol with name {dft.SymbolContainer}.{dft.SymbolName} are registered. Consider desambiguating using symbolContainer argument in {pr}"
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
                return mi;
            };

            FilePathEntity.ToAbsolute = FilePathEmbedded.ToAbsolute = url => SignumCurrentContextFilter.Url == null ? Content(url) : SignumCurrentContextFilter.Url!.Content(url);

            ReflectionServer.RegisterLike(typeof(FileMessage), () => true);
        }
    }
}
