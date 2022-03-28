using Signum.React.Facades;
using Signum.Entities.Excel;
using Microsoft.AspNetCore.Builder;
using Signum.Engine.Authorization;

namespace Signum.React.Excel;

public static class ExcelServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        ReflectionServer.RegisterLike(typeof(ExcelMessage), () => ExcelPermission.PlainExcel.IsAuthorized());
    }
}
