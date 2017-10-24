using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.React.Authorization;

namespace Signum.React.Authorization
{
    public class UserTicketServer
    {
        public static Func<string> OnCookieName = () => "sfUser";
        public static string CookieName { get { return OnCookieName(); } }

        public static bool LoginFromCookie()
        {
            using (AuthLogic.Disable())
            {
                try
                {
                    var authCookie = System.Web.HttpContext.Current.Request.Cookies[CookieName];
                    if (authCookie == null || !authCookie.Value.HasText())
                        return false;   //there is no cookie

                    string ticketText = authCookie.Value;

                    UserEntity user = UserTicketLogic.UpdateTicket(
                           System.Web.HttpContext.Current.Request.UserHostAddress,
                           ref ticketText);

                    AuthServer.OnUserPreLogin(null, user);

                    System.Web.HttpContext.Current.Response.Cookies.Add(new HttpCookie(CookieName, ticketText)
                    {
                        Expires = DateTime.UtcNow.Add(UserTicketLogic.ExpirationInterval),
                        HttpOnly = true,
                        Domain = System.Web.HttpContext.Current.Request.Url.Host

                    });

                    AuthServer.AddUserSession(user);
                    return true;
                }
                catch
                {
                    //Remove cookie
                    RemoveCookie();

                    return false;
                }
            }
        }

        public static void RemoveCookie()
        {
            HttpCookie cookie = new HttpCookie(CookieName)
            {
                Expires = DateTime.UtcNow.AddDays(-10), // or any other time in the past
                HttpOnly = true,
                Domain = System.Web.HttpContext.Current.Request.Url.Host
            };
            System.Web.HttpContext.Current.Response.Cookies.Set(cookie);
        }

        public static void SaveCookie()
        {
            string ticketText = UserTicketLogic.NewTicket(
                      System.Web.HttpContext.Current.Request.UserHostAddress);

            HttpCookie cookie = new HttpCookie(CookieName, ticketText)
            {
                Expires = DateTime.UtcNow.Add(UserTicketLogic.ExpirationInterval),
            };

            System.Web.HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }
}