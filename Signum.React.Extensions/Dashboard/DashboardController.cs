using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Engine.Basics;
using Signum.Entities.Dashboard;
using Signum.Engine.Dashboard;

namespace Signum.React.Dashboard
{
    public class DashboardController : ControllerBase
    {
        [HttpGet("api/dashboard/forEntityType/{typeName}")]
        public IEnumerable<Lite<DashboardEntity>> FromEntityType(string typeName)
        {
            return DashboardLogic.GetDashboardsEntity(TypeLogic.GetType(typeName));
        }
        [HttpGet("api/dashboard/home")]
        public Lite<DashboardEntity>? Home()
        {
            if (TypeAuthLogic.GetAllowed(typeof(DashboardEntity)).MaxUI() == TypeAllowedBasic.None)
                return null;

            var result = DashboardLogic.GetHomePageDashboard();
            return result?.ToLite();
        }
    }
}
