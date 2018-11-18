using Signum.React.Json;
using Signum.Utilities;
using System.Reflection;
using Signum.Engine.Basics;
using Signum.React.UserAssets;
using Signum.Entities.MachineLearning;
using Signum.Engine.MachineLearning;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.MachineLearning
{
    public static class PredictorServer
    {
        public static void Start(IApplicationBuilder app)
        {
            UserAssetServer.Start(app);

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
                    var qd = QueryLogic.Queries.QueryDescription(mc.Query.ToQueryName());
                    mc.ParseData(qd);
                }
            });
        }
    }
}