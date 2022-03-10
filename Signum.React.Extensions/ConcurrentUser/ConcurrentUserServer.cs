using Microsoft.AspNetCore.Builder;

namespace Signum.React.ConcurrentUser;

public static class ConcurrentUserServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
    }
}
