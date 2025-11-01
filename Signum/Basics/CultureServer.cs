using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Signum.API;
using Signum.API.Filters;
using System.Globalization;

namespace Signum.Basics;

public static class CultureServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        SignumCultureSelectorFilter.GetCurrentCulture = (context) =>
        {
            // 1 cookie (temporary)
            var lang = ReadLanguageCookie(context);
            if (lang != null)
                return CultureInfo.GetCultureInfo(lang);

            // 2 user preference
            if (UserHolder.CurrentUserCulture is { } ci)
                return ci;

            //3 requestCulture or default
            CultureInfo? ciRequest = GetCultureRequest(context);
            if (ciRequest != null)
                return ciRequest;

            return wsb.DefaultCulture; //Translation
        };
    }

    public static CultureInfo? GetCultureRequest(ActionContext actionContext)
    {
        var acceptedLanguages = actionContext.HttpContext.Request.GetTypedHeaders().AcceptLanguage;
        foreach (var lang in acceptedLanguages.Select(l => l.Value))
        {
            var culture = CultureInfoLogic.ApplicationCultures(isNeutral: false).FirstOrDefault(ci => ci.Name == lang);

            if (culture != null)
                return culture;

            string? cleanLang = lang.Value.TryBefore('-') ?? lang.Value;
            if (cleanLang != null)
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
