using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using Signum.Entities.Dashboard;

namespace Signum.Services
{
    [ServiceContract]
    public interface IDashboardServer
    {
        [OperationContract, NetDataContract]
        DashboardDN GetHomePageDashboard();

        [OperationContract, NetDataContract]
        List<Lite<DashboardDN>> GetDashboardEntity(Type entityType);

        [OperationContract, NetDataContract]
        List<Lite<DashboardDN>> AutocompleteDashboard(string subString, int limit);
    }
}
