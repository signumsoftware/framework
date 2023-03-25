using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace  Signum.Toolbar;

public static class ToolbarServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
    }
}
