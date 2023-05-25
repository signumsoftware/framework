using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.Authorization;
using Signum.Dynamic.Views;
using Signum.Eval.TypeHelp;

namespace Signum.Dynamic;

public static class DynamicServer
{
    public static void Start(IApplicationBuilder app)
    {
        ReflectionServer.RegisterLike(typeof(DynamicPanelPermission), () => DynamicPanelPermission.RestartApplication.IsAuthorized());
    }
}
