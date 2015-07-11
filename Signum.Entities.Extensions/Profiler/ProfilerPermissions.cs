using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.Authorization;

namespace Signum.Entities.Profiler
{
    [AutoInit]
    public static class ProfilerPermission
    {
        public static PermissionSymbol ViewTimeTracker;
        public static PermissionSymbol ViewHeavyProfiler;
        public static PermissionSymbol OverrideSessionTimeout;
    }
}
