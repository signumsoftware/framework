using Signum.Engine.Authorization;
using Signum.Entities.Omnibox;

namespace Signum.Engine.Omnibox;

public static class OmniboxLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            PermissionAuthLogic.RegisterTypes(typeof(OmniboxPermission));
        }
    }
}
