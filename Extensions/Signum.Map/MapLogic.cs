
using Signum.API;
using Signum.Omnibox;

namespace Signum.Map;

public static class MapLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            PermissionLogic.RegisterPermissions(MapPermission.ViewMap);

            if (sb.WebServerBuilder != null)
            {
                MapServer.Start(sb.WebServerBuilder);
                OmniboxParser.Generators.Add(new MapOmniboxResultGenerator(type => OperationLogic.TypeOperations(type).Any()));
            }
        }
    }
}
