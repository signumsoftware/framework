using Signum.Entities.Profiler;
using Signum.Windows.Omnibox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Windows.Profiler
{
    public class ProfilerClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ClientProfiler",
                    () => true,
                    win =>
                    {
                        ProfilerUploader.OpenProfilerUploader();
                    }));
            }
        }
    }
}
