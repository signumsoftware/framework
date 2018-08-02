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
using Signum.Entities.UserQueries;
using Signum.Engine.Basics;
using Signum.React.UserAssets;
using Signum.React.Facades;
using Signum.Engine.UserQueries;
using Signum.Engine.Authorization;

namespace Signum.React.UserQueries
{
    public static class UserQueryServer
    {
        public static void Start(HttpConfiguration config)
        {
            UserAssetServer.Start(config);

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
                    ep.Extension.Add("userQueries", userQueries);
            };

        }
    }
}