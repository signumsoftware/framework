using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.UserAssets;

namespace Signum.MachineLearning;

public static class PredictorServer
{
    public static void Start(WebServerBuilder app)
    {
        if (app.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        UserAssetServer.Start(app);

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
