using Signum.API;

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
