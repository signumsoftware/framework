using Signum.Entities.UserAssets;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.React.UserAssets;

namespace Signum.React.Dashboard
{
    public static class DashboardServer
    {
        public static void Start(HttpConfiguration config)
        {
            UserAssetServer.Start(config);

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        }
    }
}