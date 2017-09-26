using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.React.ApiControllers;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Signum.Engine.Basics;
using Signum.Entities.UserAssets;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Entities.Dashboard;
using Signum.Engine.Dashboard;

namespace Signum.React.Dashboard
{
    public class DashboardController : ApiController
    {
        [Route("api/dashboard/forEntityType/{typeName}"), HttpGet]
        public IEnumerable<Lite<DashboardEntity>> FromEntityType(string typeName)
        {
            return DashboardLogic.GetDashboardsEntity(TypeLogic.GetType(typeName));
        }
        [Route("api/dashboard/home"), HttpGet]
        public Lite<DashboardEntity> Home()
        {
            if (TypeAuthLogic.GetAllowed(typeof(DashboardEntity)).MaxUI() == TypeAllowedBasic.None)
                return null;

            var result = DashboardLogic.GetHomePageDashboard();
            return result?.ToLite();
        }
    }
}