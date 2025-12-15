using Microsoft.AspNetCore.CookiePolicy;
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
            var ciCookie = GetCultureFromLanguageCookie(context.HttpContext);
            if (ciCookie != null)
                return ciCookie;

            // 2 user preference
            var ciUser = UserHolder.CurrentUserCulture;
            if (ciUser != null )
                return ciUser;

            //3 HttpRequest.AcceptLanguage
            CultureInfo? ciRequest = GetCultureFromAcceptedLanguage(context.HttpContext);
            if (ciRequest != null)
                return ciRequest;

            return wsb.DefaultCulture; //4 default
        };
    }

    public static bool PreferNeutralCultureForUsers = false;

    public static CultureInfo? GetCultureFromAcceptedLanguage(HttpContext httpContext)
    {
        var acceptedLanguages = httpContext.Request.GetTypedHeaders().AcceptLanguage;
        foreach (var lang in acceptedLanguages.Select(l => l.Value))
        {
            var culture = CultureInfoLogic.ApplicationCultures(isNeutral: PreferNeutralCultureForUsers).FirstOrDefault(ci => ci.Name == lang);

            if (culture != null)
                return culture;

            string? cleanLang = lang.Value.TryBefore('-') ?? lang.Value;
            if (cleanLang != null)
            {
                culture = CultureInfoLogic.ApplicationCultures(isNeutral: PreferNeutralCultureForUsers).FirstOrDefault(ci => ci.Name.StartsWith(cleanLang));

                if (culture != null)
                    return culture;
            }
        }
        return null;
    }

    public static CultureInfo? GetCultureFromLanguageCookie(HttpContext httpContext)
    {
        var lang = httpContext.Request.Cookies.TryGetValue("language", out string? value) ? value : null;

        if(lang != null)
            return CultureInfo.GetCultureInfo(lang);

        return null;
    }

    public static CultureInfoEntity? InferUserCulture(HttpContext httpContext)
    {
        return (GetCultureFromLanguageCookie(httpContext) ?? GetCultureFromAcceptedLanguage(httpContext))?.TryGetCultureInfoEntity();
    }

}
