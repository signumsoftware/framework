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

namespace Signum.React.Files
{
    public static class FilesServer
    {
        public static void Start(IApplicationBuilder app)
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

            FilePathEntity.ToAbsolute = FilePathEmbedded.ToAbsolute = url => SignumCurrentContextFilter.Url!.Content(url);
        }
    }
}
