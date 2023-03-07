using Microsoft.AspNetCore.Builder;
using Signum.React;

namespace Signum.React.WhatsNew;

public static class WhatsNewServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodBase.GetCurrentMethod());
    }

}
