
using Signum.API;
using Signum.Utilities;

namespace Signum.Omnibox;

public static class OmniboxLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        PermissionLogic.RegisterTypes(typeof(OmniboxPermission));

        if (sb.WebServerBuilder != null)
        {
            OmniboxServer.Start(sb.WebServerBuilder);

            OmniboxParser.Generators.Add(new EntityOmniboxResultGenenerator());
            OmniboxParser.Generators.Add(new DynamicQueryOmniboxResultGenerator());
            OmniboxParser.Generators.Add(new ReactSpecialOmniboxGenerator());
        }
    }
}
