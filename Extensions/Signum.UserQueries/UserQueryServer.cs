using Signum.UserAssets;
using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.Dashboard;

namespace Signum.UserQueries;

public static class UserQueryServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        UserAssetServer.Start(wsb);
        UserAssetServer.QueryPermissionSymbols.Add(UserQueryPermission.ViewUserQuery);

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((UserQueryEntity uq) =>
        {
            if (uq.Query != null)
            {
                var qd = QueryLogic.Queries.QueryDescription(uq.Query.ToQueryName());
                uq.ParseData(qd);
            }
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
