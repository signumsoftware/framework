
using Signum.API;

namespace Signum.Omnibox;

public static class OmniboxLogic
{
    public static void Start(SchemaBuilder sb, WebServerBuilder? wsb, params IOmniboxResultGenerator[] generators)
    {
        if (wsb != null)
            OmniboxServer.Start(wsb.ApplicationBuilder, generators);

        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            PermissionLogic.RegisterTypes(typeof(OmniboxPermission));
        }
    }
}
