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
using Signum.React.Facades;
using Signum.Engine.Dashboard;
using Signum.Entities.Dashboard;
using Signum.Engine.Authorization;

namespace Signum.React.Dashboard
{
    public static class DashboardServer
    {
        public static void Start(HttpConfiguration config)
        {
            UserAssetServer.Start(config);

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            EntityPackTS.AddExtension += ep =>
            {
                if (ep.entity.IsNew || !DashboardPermission.ViewDashboard.IsAuthorized())
                    return;

                var dashboards = DashboardLogic.GetDashboardsEntity(ep.entity.GetType());
                if (dashboards.Any())
                    ep.Extension.Add("dashboards", dashboards);
          
                var result = DashboardLogic.GetEmbeddedDashboard(ep.entity.GetType());
                if (result != null)
                    ep.Extension.Add("embeddedDashboard", result);
            };
        }
    }
}