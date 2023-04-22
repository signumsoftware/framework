
using Signum.API;

namespace Signum.Omnibox;

public static class OmniboxLogic
{
    public static void Start(SchemaBuilder sb, WebServerBuilder? wsb, params IOmniboxResultGenerator[] generators)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            PermissionLogic.RegisterTypes(typeof(OmniboxPermission));

            if (wsb != null)
                OmniboxServer.Start(wsb.WebApplication, generators);
        }
    }
}
