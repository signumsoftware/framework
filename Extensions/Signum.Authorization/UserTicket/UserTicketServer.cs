using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Signum.Authorization.UserTicket;

public class UserTicketServer
{
    public static Func<string> OnCookieName = () => "sfUser";
    public static string CookieName { get { return OnCookieName(); } }
    public static Action<ActionContext> OnSaveCookie = ac => SaveCookie(ac);

    public static bool LoginFromCookie(ActionContext ac)
    {
        using (AuthLogic.Disable())
        {
            try
            {
                if (!ac.HttpContext.Request.Cookies.TryGetValue(CookieName, out string? ticketText) || !ticketText.HasText())
                    return false;   //there is no cookie

                var httpConnection = ac.HttpContext.Features.Get<IHttpConnectionFeature>()!;

                UserEntity user = UserTicketLogic.UpdateTicket(httpConnection.RemoteIpAddress!.ToString(), ref ticketText);

                AuthServer.OnUserPreLogin(ac, user);

                ac.HttpContext.Response.Cookies.Append(CookieName, ticketText, new CookieOptions
                {
                    Domain = ac.HttpContext.Request.Host.Host.ToString(),
                    Path = new UrlHelper(ac).Content("~/"),
                    Expires = DateTime.UtcNow.Add(UserTicketLogic.ExpirationInterval),
                });

                AuthServer.AddUserSession(ac, user);
                return true;
            }
            catch
            {
                //Remove cookie
                RemoveCookie(ac);

                return false;
            }
        }
    }

    public static void RemoveCookie(ActionContext ac)
    {
        ac.HttpContext.Response.Cookies.Append(CookieName, "", new CookieOptions
        {
            Domain = ac.HttpContext.Request.Host.Host,
            Path = new UrlHelper(ac).Content("~/"),
            Expires = DateTime.Now.AddDays(-1)
        });
    }

    public static void SaveCookie(ActionContext ac)
    {
        var httpConnection = ac.HttpContext.Features.Get<IHttpConnectionFeature>()!;

        string ticketText = UserTicketLogic.NewTicket(httpConnection.LocalIpAddress!.ToString());

        ac.HttpContext.Response.Cookies.Append(CookieName, ticketText, new CookieOptions
        {
            Domain = ac.HttpContext.Request.Host.Host,
            Path = new UrlHelper(ac).Content("~/"),
            Expires = DateTime.UtcNow.Add(UserTicketLogic.ExpirationInterval),
        });
    }
}
