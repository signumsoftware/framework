using Signum.Engine.Basics;
using Signum.Engine.Translation;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Signum.React.Facades;
using Signum.Entities.Translation;
using Microsoft.AspNetCore.Builder;
using Signum.Engine.Authorization;
using Signum.Utilities;
using Signum.Engine.Json;
using System.Text.Json;
using Signum.Entities.UserQueries;
using Signum.Entities;
using Microsoft.AspNetCore.Http;

namespace Signum.React.Translation
{
    public class TranslationServer
    {
        public static ITranslator[] Translators;

        public static void Start(IApplicationBuilder app, params ITranslator[] translators)
        {
            Translators = translators;
            
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            ReflectionServer.RegisterLike(typeof(TranslationMessage), () => TranslationPermission.TranslateCode.IsAuthorized() || TranslationPermission.TranslateInstances.IsAuthorized());

            ReflectionServer.PropertyRouteExtension += (mi, pr) =>
            {
                var type = TranslatedInstanceLogic.TranslateableRoutes.TryGetC(pr.RootType)?.TryGetS(pr);
                if (type != null)
                {
                    mi.Extension.Add("translatable", true);
                }
                return mi;
            };

            var pairs = TranslatedInstanceLogic.TranslateableRoutes.Values.SelectMany(a => a.Keys)
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

                        if (TranslatedInstanceLogic.RouteType(pr) == null)
                            return;

                        var discard = reader.GetString();
                    },
                    CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
                    {
                        var pr = ctx.ParentPropertyRoute.Add(prop);

                        if (TranslatedInstanceLogic.RouteType(pr) == null)
                            return;

                        var hastMList = pr.GetMListItemsRoute() != null;

                        var entity = ctx.Entity as Entity ?? (Entity?)EntityJsonContext.FindCurrentRootEntity();

                        var rowId = hastMList ? EntityJsonContext.FindCurrentRowId() : null;

                        writer.WritePropertyName(ctx.LowerCaseName);

                        var value = entity == null || entity.IsNew || hastMList && rowId == null /*UserQuery apply changes*/ ? null :
                        TranslatedInstanceLogic.TranslatedField(entity.ToLite(), pr, rowId, null!);

                        writer.WriteStringValue(value);
                    }
                });

            }
        }

        public static CultureInfo? GetCultureRequest(ActionContext actionContext)
        {
            var acceptedLanguages = actionContext.HttpContext.Request.GetTypedHeaders().AcceptLanguage;
            foreach (var lang in acceptedLanguages.Select(l => l.Value))
            {
                var culture = CultureInfoLogic.ApplicationCultures(isNeutral: false).FirstOrDefault(ci => ci.Name == lang);

                if (culture != null)
                    return culture;

                string? cleanLang = lang.Value.TryBefore('-');
                if(cleanLang != null)
                {
                    culture = CultureInfoLogic.ApplicationCultures(isNeutral: false).FirstOrDefault(ci => ci.Name.StartsWith(cleanLang));

                    if (culture != null)
                        return culture;
                }
            }
            return null;
        }

        public static string? ReadLanguageCookie(ActionContext ac)
        {
            return ac.HttpContext.Request.Cookies.TryGetValue("language", out string? value) ? value : null;
        }
    }
}
