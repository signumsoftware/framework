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

namespace Signum.React.Translation
{
    public class TranslationServer
    {
        public static ITranslator Translator;

        public static void Start(IApplicationBuilder app, ITranslator translator)
        {
            ReflectionServer.RegisterLike(typeof(TranslationMessage), () => TranslationPermission.TranslateCode.IsAuthorized() || TranslationPermission.TranslateInstances.IsAuthorized());

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            Translator = translator;
        }

        public static CultureInfo? GetCultureRequest(ActionContext actionContext)
        {
            var acceptedLanguages = actionContext.HttpContext.Request.GetTypedHeaders().AcceptLanguage;
            foreach (var lang in acceptedLanguages.Select(l => l.Value))
            {
                var cleanLang = lang.TryBefore('-');

                if (cleanLang != null)
                {
                    culture = CultureInfoLogic.ApplicationCultures.FirstOrDefault(ci => ci.Name.StartsWith(cleanLang));

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
