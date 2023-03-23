using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Processes;

public static class ProcessServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

    }
}
