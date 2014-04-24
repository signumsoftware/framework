using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.Authorization;

namespace Signum.Entities.Profiler
{
    public static class ProfilerPermission
    {
        public static readonly PermissionSymbol ViewTimeTracker = new PermissionSymbol();
        public static readonly PermissionSymbol ViewHeavyProfiler = new PermissionSymbol();
        public static readonly PermissionSymbol OverrideSessionTimeout = new PermissionSymbol();
    }
}
