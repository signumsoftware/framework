
using Signum.API;

namespace Signum.Map;

public static class MapLogic
{
    public static void Start(SchemaBuilder sb, WebServerBuilder? wsb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            PermissionLogic.RegisterPermissions(MapPermission.ViewMap);

            if (wsb != null)
                MapServer.Start(wsb.WebApplication);
        }
    }
}
