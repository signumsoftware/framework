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
        DashboardEntity GetHomePageDashboard();

        [OperationContract, NetDataContract]
        DashboardEntity GetEmbeddedDashboard(Type entityType);

        [OperationContract, NetDataContract]
        List<Lite<DashboardEntity>> GetDashboardsEntity(Type entityType);

        [OperationContract, NetDataContract]
        List<Lite<DashboardEntity>> GetDashboards();

        [OperationContract, NetDataContract]
        DashboardEntity RetrieveDashboard(Lite<DashboardEntity> dashboard);

        [OperationContract, NetDataContract]
        List<Lite<DashboardEntity>> AutocompleteDashboard(string subString, int limit);
    }
}
