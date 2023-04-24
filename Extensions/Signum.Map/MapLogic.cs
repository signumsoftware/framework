
using Signum.API;

namespace Signum.Map;

public static class MapLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            PermissionLogic.RegisterPermissions(MapPermission.ViewMap);

            if (sb.WebServerBuilder != null)
                MapServer.Start(sb.WebServerBuilder.WebApplication);
        }
    }
}
