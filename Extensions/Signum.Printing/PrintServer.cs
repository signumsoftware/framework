using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Printing;

public static class PrintServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
    }
}
