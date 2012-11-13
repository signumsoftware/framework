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
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<ExceptionDN>(EntityType.System) { View = e => new ExceptionCtrl() });
            }
        }
    }
}
