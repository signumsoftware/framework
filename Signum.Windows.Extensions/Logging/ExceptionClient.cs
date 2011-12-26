using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Entities.Logging;

namespace Signum.Windows.Logging
{
    public static class LoggingClient
    {
        public static void Start(bool exceptions, bool deployment)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (exceptions)
                    Navigator.AddSetting(new EntitySettings<ExceptionDN>(EntityType.ServerOnly) { View = e => new ExceptionCtrl() });

                if (deployment)
                    Navigator.AddSetting(new EntitySettings<DeploymentLogDN>(EntityType.ServerOnly) { View = e => new DeploymentLog() });
            }
        }
    }
}
