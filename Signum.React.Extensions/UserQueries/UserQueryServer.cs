using Signum.React.Json;
using Signum.Utilities;
using System.Linq;
using System.Reflection;
using Signum.Entities.UserQueries;
using Signum.Engine.Basics;
using Signum.React.UserAssets;
using Signum.React.Facades;
using Signum.Engine.UserQueries;
using Signum.Engine.Authorization;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.UserQueries
{
    public static class UserQueryServer
    {
        public static void Start(IApplicationBuilder app)
        {
            UserAssetServer.Start(app);

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            EntityJsonConverter.AfterDeserilization.Register((UserQueryEntity uq) =>
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
}