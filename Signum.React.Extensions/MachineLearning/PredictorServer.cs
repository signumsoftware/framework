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
using Signum.Entities.MachineLearning;
using Signum.Engine.MachineLearning;

namespace Signum.React.MachineLearning
{
    public static class PredictorServer
    {
        public static void Start(HttpConfiguration config)
        {
            UserAssetServer.Start(config);

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            EntityJsonConverter.AfterDeserilization.Register((PredictorMainQueryEmbedded p) =>
            {
                if (p.Query != null)
                {
                    p.ParseData();
                }
            });

            EntityJsonConverter.AfterDeserilization.Register((PredictorSubQueryEntity mc) =>
            {
                if (mc.Query != null)
                {
                    var qd = DynamicQueryManager.Current.QueryDescription(mc.Query.ToQueryName());
                    mc.ParseData(qd);
                }
            });
        }
    }
}