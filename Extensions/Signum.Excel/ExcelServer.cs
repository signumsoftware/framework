using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.Excel;

namespace Signum.Excel;

public static class ExcelServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ReflectionServer.RegisterLike(typeof(ExcelMessage), () => ExcelPermission.PlainExcel.IsAuthorized());
    }
}
