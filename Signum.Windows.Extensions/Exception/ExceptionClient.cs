using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Entities.Exceptions;
using Signum.Entities.Deployment;

namespace Signum.Windows.Logging
{
    public static class ExceptionClient
    {
        public static void Start(bool exceptions, bool deployment)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<ExceptionDN>(EntityType.ServerOnly) { View = e => new ExceptionCtrl() });
            }
        }
    }
}
