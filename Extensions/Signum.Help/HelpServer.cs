using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Help;

public static class HelpServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
    }
}
