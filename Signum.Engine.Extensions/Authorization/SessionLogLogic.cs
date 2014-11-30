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
using Signum.Entities.Basics;

namespace Signum.Engine.Authorization
{
    public static class SessionLogLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);

                sb.Include<SessionLogEntity>();

                PermissionAuthLogic.RegisterPermissions(SessionLogPermission.TrackSession);

                dqm.RegisterQuery(typeof(SessionLogEntity), () =>
                    from sl in Database.Query<SessionLogEntity>()
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

        public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEntity parameters)
        {
            Database.Query<SessionLogEntity>().Where(a => a.SessionStart < parameters.DateLimit).UnsafeDeleteChunks(parameters.ChunkSize, parameters.MaxChunks);
        }

        static bool RoleTracked(Lite<RoleEntity> role)
        {
            return SessionLogPermission.TrackSession.IsAuthorized(role);
        }

        public static void SessionStart(string userHostAddress, string userAgent)
        {
            var user = UserEntity.Current;
            if (SessionLogLogic.RoleTracked(user.Role.ToLite()))
            {
                using (AuthLogic.Disable())
                {
                    new SessionLogEntity
                    {
                        User = user.ToLite(),
                        SessionStart = TimeZoneManager.Now.TrimToSeconds(),
                        UserHostAddress = userHostAddress,
                        UserAgent = userAgent
                    }.Save();
                }
            }
        }

        public static void SessionEnd(UserEntity user, TimeSpan? timeOut)
        {
            if (user == null || !RoleTracked(user.Role.ToLite()))
                return;

            using (AuthLogic.Disable())
            {
                var sessionEnd = timeOut.HasValue ? TimeZoneManager.Now.Subtract(timeOut.Value).TrimToSeconds() : TimeZoneManager.Now.TrimToSeconds();

                var rows = Database.Query<SessionLogEntity>()
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
