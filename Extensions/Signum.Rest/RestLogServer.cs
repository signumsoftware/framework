using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Rest;

public static class RestLogServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());


    }
}
