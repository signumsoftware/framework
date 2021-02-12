using Signum.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Signum.React.Maps;
using Signum.React.Facades;
using Signum.Engine.Cache;
using Signum.Entities.Cache;
using Signum.Engine.Authorization;
using Signum.Engine.Maps;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.Alerts
{
    public static class AlertsServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        }
    }
}
