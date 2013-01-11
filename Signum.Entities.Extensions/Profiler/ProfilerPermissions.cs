using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace Signum.Entities.Profiler
{
    public enum ProfilerPermission
    {
        ViewTimeTracker,
        ViewHeavyProfiler, 
        OverrideSessionTimeout
    }
}
