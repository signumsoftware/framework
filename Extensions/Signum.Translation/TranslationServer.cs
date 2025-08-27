using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Signum.Translation;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Signum.Translation.Translators;
using Signum.Translation.Instances;
using Signum.API;
using Signum.API.Json;
using Signum.API.Filters;
using Signum.Authorization;

namespace Signum.Translation;

public class TranslationServer
{

    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ReflectionServer.RegisterLike(typeof(TranslationMessage), () => TranslationPermission.TranslateCode.IsAuthorized() || TranslationPermission.TranslateInstances.IsAuthorized());

        ReflectionServer.PropertyRouteExtension += (mi, pr) =>
        {
            var type = PropertyRouteTranslationLogic.TranslateableRoutes.TryGetC(pr.RootType)?.TryGetS(pr);
            if (type != null)
            {
                mi.Extension.Add("translatable", true);
            }
            return mi;
        };

        Schema.Current.BeforeDatabaseAccess += () =>
        {
            var pairs = PropertyRouteTranslationLogic.TranslateableRoutes.Values.SelectMany(a => a.Keys)
                 .Select(pr => (type: pr.Parent!.Type, prop: pr.PropertyInfo!))
                 .Distinct()
                 .ToList();

            foreach (var (type, prop) in pairs)
            {
                var converters = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(type);

                converters.Add(prop.Name.FirstLower() + "_translated", new PropertyConverter()
                {
                    AvoidValidate = true,
                    CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
                    {
                        var pr = ctx.ParentPropertyRoute.Add(prop);

                        if (PropertyRouteTranslationLogic.RouteType(pr) == null)
                            return;

                        var discard = reader.GetString();
                    },
                    CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
                    {
                        var pr = ctx.ParentPropertyRoute.Add(prop);

                        if (PropertyRouteTranslationLogic.RouteType(pr) == null)
                            return;

                        var hastMList = pr.GetMListItemsRoute() != null;

                        var path = EntityJsonContext.CurrentSerializationPath;

                        var entity = ctx.Entity as Entity ?? (Entity?)path?.CurrentRootEntity();

                        var rowId = hastMList ? path?.CurrentRowId() : null;

                        writer.WritePropertyName(ctx.LowerCaseName);

                        var value = entity == null || entity.IsNew || hastMList && rowId == null /*UserQuery apply changes*/ ? null :
                        PropertyRouteTranslationLogic.TranslatedField(entity.ToLite(), pr, rowId, null!);

                        writer.WriteStringValue(value);
                    }
                });
            }
        };
    }


}
