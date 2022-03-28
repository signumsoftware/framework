using Signum.Engine.Authorization;
using Signum.Entities.Map;

namespace Signum.Engine.Map;

public static class MapLogic
{
   
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            PermissionAuthLogic.RegisterPermissions(MapPermission.ViewMap);
        }
    }
}
