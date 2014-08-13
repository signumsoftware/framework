using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Engine.Basics;

namespace Signum.Engine.Authorization
{
    public static class SessionLogLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);

                sb.Include<SessionLogDN>();

                PermissionAuthLogic.RegisterPermissions(SessionLogPermission.TrackSession);

                dqm.RegisterQuery(typeof(SessionLogDN), () =>
                    from sl in Database.Query<SessionLogDN>()
                    select new
                    {
                        Entity = sl,
                        sl.Id,
                        sl.User,
                        sl.SessionStart,
                        sl.SessionEnd,
                        sl.SessionTimeOut
                    });

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        public static void ExceptionLogic_DeleteLogs(DateTime limite)
        {
            Database.Query<SessionLogDN>().Where(a => a.SessionStart < limite).UnsafeDelete();
        }

        static bool RoleTracked(Lite<RoleDN> role)
        {
            return SessionLogPermission.TrackSession.IsAuthorized(role);
        }

        public static void SessionStart(string userHostAddress, string userAgent)
        {
            var user = UserDN.Current;
            if (SessionLogLogic.RoleTracked(user.Role.ToLite()))
            {
                using (AuthLogic.Disable())
                {
                    new SessionLogDN
                    {
                        User = user.ToLite(),
                        SessionStart = TimeZoneManager.Now.TrimToSeconds(),
                        UserHostAddress = userHostAddress,
                        UserAgent = userAgent
                    }.Save();
                }
            }
        }

        public static void SessionEnd(UserDN user, TimeSpan? timeOut)
        {
            if (user == null || !RoleTracked(user.Role.ToLite()))
                return;

            using (AuthLogic.Disable())
            {
                var sessionEnd = timeOut.HasValue ? TimeZoneManager.Now.Subtract(timeOut.Value).TrimToSeconds() : TimeZoneManager.Now.TrimToSeconds();

                var rows = Database.Query<SessionLogDN>()
                    .Where(sl => sl.User.RefersTo(user))
                    .OrderByDescending(sl => sl.SessionStart)
                    .Take(1)
                    .Where(sl => sl.SessionEnd == null)
                    .UnsafeUpdate()
                    .Set(a => a.SessionEnd, a => sessionEnd)
                    .Set(a => a.SessionTimeOut, a => timeOut.HasValue)
                    .Execute();
            }
        }
    }
}
