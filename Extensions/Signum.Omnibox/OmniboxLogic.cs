
using Signum.API;

namespace Signum.Omnibox;

public static class OmniboxLogic
{
    public static void Start(SchemaBuilder sb, params IOmniboxResultGenerator[] generators)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            PermissionLogic.RegisterTypes(typeof(OmniboxPermission));

            if (sb.WebServerBuilder != null)
                OmniboxServer.Start(sb.WebServerBuilder.WebApplication, generators);
        }
    }
}
