using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Authorization;
using Signum.Entities.Profiler;

namespace Signum.Engine.Profiler
{
    public static class ProfilerLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool timeTracker, bool heavyProfiler)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (timeTracker)
                    PermissionAuthLogic.RegisterPermissions(ProfilerPermissions.ViewTimeTracker);


                if (heavyProfiler)
                    PermissionAuthLogic.RegisterPermissions(ProfilerPermissions.ViewHeavyProfiler); 
            }
        }
    }
}
