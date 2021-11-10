using Microsoft.AspNetCore.Builder;

namespace Signum.React.Alerts;

public static class AlertsServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

    }
}
