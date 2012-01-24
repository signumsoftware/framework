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

namespace Signum.Engine.Authorization
{
    public static class SessionLogLogic
    {
        static List<RoleDN> rolesTracked;
        
        public static bool RoleTracked(RoleDN role)
        {
            return rolesTracked.Contains(role);
        }
                 
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, List<RoleDN> rolesToTrack)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                rolesTracked = rolesToTrack;

                AuthLogic.AssertStarted(sb);
                sb.Include<SessionLogDN>();

                dqm[typeof(SessionLogDN)] = (from sl in Database.Query<SessionLogDN>()
                                             select new
                                             {
                                                 Entity = sl.ToLite(),
                                                 sl.Id,
                                                 sl.User,
                                                 sl.SessionStart,
                                                 sl.SessionEnd,
                                                 sl.SessionTimeOut
                                             }).ToDynamic();
            }
        }

        public static void FinishSession(UserDN user, bool sessionTimeOut)
        {
            if (!RoleTracked(user.Role))
                return;

            using (AuthLogic.Disable())
            {
                var log = Database.Query<SessionLogDN>()
                    .Where(sl => sl.User.RefersTo(user))
                    .OrderByDescending(sl => sl.SessionStart)
                    .FirstOrDefault();
                
                if (log != null && log.SessionEnd == null)
                {
                    log.SessionEnd = DateTime.Now.TrimToSeconds();
                    log.SessionTimeOut = sessionTimeOut;
                    log.Save();
                }
            }
        }
    }
}
