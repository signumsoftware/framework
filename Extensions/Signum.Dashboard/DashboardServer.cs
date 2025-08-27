using Signum.UserAssets;
using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Dashboard;

public static class DashboardServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        UserAssetServer.Start(wsb);
        UserAssetServer.QueryPermissionSymbols.Add(DashboardPermission.ViewDashboard);

        EntityPackTS.AddExtension += ep =>
        {
            if (ep.entity.IsNew || !DashboardPermission.ViewDashboard.IsAuthorized())
                return;

            var dashboards = DashboardLogic.GetDashboards(ep.entity.GetType());
            if (dashboards.Any())
                ep.extension.Add("dashboards", dashboards);

            var result = DashboardLogic.GetEmbeddedDashboards(ep.entity.GetType());
            if (result != null)
                ep.extension.Add("embeddedDashboards", result);
        };

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((DashboardEntity d) =>
        {
            d.ParseData(q => QueryLogic.Queries.QueryDescription(q.ToQueryName()));
        });
    }
}
