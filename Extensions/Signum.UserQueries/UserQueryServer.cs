using Signum.UserAssets;
using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.Dashboard;

namespace Signum.UserQueries;

public static class UserQueryServer
{
    public static void Start(IApplicationBuilder app)
    {
        UserAssetServer.Start(app);
        UserAssetServer.QueryPermissionSymbols.Add(UserQueryPermission.ViewUserQuery);

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((UserQueryEntity uq) =>
        {
            if (uq.Query != null)
            {
                var qd = QueryLogic.Queries.QueryDescription(uq.Query.ToQueryName());
                uq.ParseData(qd);
            }
        });

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((BigValuePartEntity uq) =>
        {
            uq.ParseData(uq.GetDashboard());
        });

        EntityPackTS.AddExtension += ep =>
        {
            if (ep.entity.IsNew || !UserQueryPermission.ViewUserQuery.IsAuthorized())
                return;

            var userQueries = UserQueryLogic.GetUserQueries(ep.entity.GetType());
            if (userQueries.Any())
                ep.extension.Add("userQueries", userQueries);
        };
    }
}
