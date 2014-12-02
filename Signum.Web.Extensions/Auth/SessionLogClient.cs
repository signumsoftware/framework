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
                AuthController.UserLogged += new Action(LogSessionStart);
                AuthController.UserLoggingOut += new Action(AuthController_OnUserLoggingOut);
            }
        }

        static void LogSessionStart()
        {
            var request = HttpContext.Current.Request;
            SessionLogLogic.SessionStart(request.UserHostAddress, request.UserAgent);
        }

        static void AuthController_OnUserLoggingOut()
        {
            LogSessionEnd(UserEntity.Current, null);
        }

        public static void LogSessionEnd(UserEntity user, TimeSpan? timeOut)
        {
            SessionLogLogic.SessionEnd(user, timeOut);
        }
    }
}