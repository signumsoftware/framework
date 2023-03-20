using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Eval.TypeHelp;

public static class TypeHelpServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodBase.GetCurrentMethod());
    }
}
