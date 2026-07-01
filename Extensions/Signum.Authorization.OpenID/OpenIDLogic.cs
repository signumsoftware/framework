using Signum.API;
using Signum.Authorization.BaseAD;
using Signum.Utilities.Reflection;

namespace Signum.Authorization.OpenID;

public static class OpenIDLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        Lite.RegisterLiteModelConstructor((UserEntity u) => new UserLiteModel
        {
            UserName = u.UserName,
            ToStringValue = u.ToString(),
            ExternalId = u.ExternalId,
        });
        ReflectionServer.RegisterLike(typeof(UserADMessage), () => true);
        ReflectionServer.RegisterLike(typeof(OpenIDMessage), () => true);
    }
}
