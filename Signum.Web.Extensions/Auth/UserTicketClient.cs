using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Web.Auth;

namespace Signum.Web.Auth
{
    public class UserTicketClient
    {
        //public static string CookieName = "sfUser";
        public static Func<string> CookieNameFunc = () => "sfUser";
        public static string CookieName { get { return CookieNameFunc(); } }

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

                    AuthController.OnUserPreLogin(null, user); 

                    System.Web.HttpContext.Current.Response.Cookies.Add(new HttpCookie(CookieName, ticketText)
                    {
                        Expires = DateTime.UtcNow.Add(UserTicketLogic.ExpirationInterval),
                    });

                    AuthController.AddUserSession(user);
                    return true;
                }
                catch
                {
                    //Remove cookie
                    HttpCookie cookie = new HttpCookie(CookieName)
                    {
                        Expires = DateTime.UtcNow.AddDays(-10) // or any other time in the past
                    };
                    System.Web.HttpContext.Current.Response.Cookies.Set(cookie);

                    return false;
                }
            }
        }

        public static void RemoveCookie()
        {
            var httpContext = System.Web.HttpContext.Current;

            var authCookie = httpContext.Request.Cookies[CookieName];
            if (authCookie != null && authCookie.Value.HasText())
                httpContext.Response.Cookies[CookieName].Expires = DateTime.UtcNow.AddDays(-10);
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