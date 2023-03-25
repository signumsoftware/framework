using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.WhatsNew;

public static class WhatsNewServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodBase.GetCurrentMethod());
    }

}
