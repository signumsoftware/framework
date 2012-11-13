using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Entities.Exceptions;
using Signum.Entities.Deployment;

namespace Signum.Windows.Deployment
{
    public static class DeploymentClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<DeploymentLogDN>(EntityType.System) { View = e => new DeploymentLog() });
            }
        }
    }
}
