
namespace Signum.Omnibox;

public static class OmniboxLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            PermissionLogic.RegisterTypes(typeof(OmniboxPermission));
        }
    }
}
