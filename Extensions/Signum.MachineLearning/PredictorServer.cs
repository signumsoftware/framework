using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.React.UserAssets;

namespace Signum.MachineLearning;

public static class PredictorServer
{
    public static void Start(IApplicationBuilder app)
    {
        UserAssetServer.Start(app);

        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((PredictorMainQueryEmbedded p) =>
        {
            if (p.Query != null)
            {
                p.ParseData();
            }
        });

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((PredictorSubQueryEntity mc) =>
        {
            if (mc.Query != null)
            {
                var qd = QueryLogic.Queries.QueryDescription(mc.Query.ToQueryName());
                mc.ParseData(qd);
            }
        });
    }
}
