using Signum.UserAssets;
using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.UserQueries;

public static class UserQueryServer
{
    public static void Start(IApplicationBuilder app)
    {
        UserAssetServer.Start(app);

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

            var userQueries = UserQueryLogic.GetUserQueriesEntity(ep.entity.GetType());
            if (userQueries.Any())
                ep.extension.Add("userQueries", userQueries);
        };
    }
}
