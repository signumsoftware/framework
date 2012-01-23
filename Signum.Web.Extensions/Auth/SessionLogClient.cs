using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine;

namespace Signum.Web.Auth
{
    public static class SessionLogClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthController.OnUserLogged += new Action(LogSessionStart);
                AuthController.OnUserLoggingOut += new Action(AuthController_OnUserLoggingOut);
            }
        }

        static void LogSessionStart()
        {
            var user = UserDN.Current;
            if (SessionLogLogic.RoleTracked(user.Role))
            {
                var request = HttpContext.Current.Request;
                new SessionLogDN
                {
                    User = user.ToLite(),
                    SessionStart = DateTime.Now.TrimToSeconds(),
                    UserHostAddress = request.UserHostAddress,
                    UserAgent = request.UserAgent
                }.Save();
            }
        }

        static void AuthController_OnUserLoggingOut()
        {
            LogSessionEnd(UserDN.Current, false);
        }

        public static void LogSessionEnd(UserDN user, bool sessionTimeOut)
        {
            if (user != null)
                SessionLogLogic.FinishSession(user, sessionTimeOut);
        }
    }
}