using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.Authorization;
using Signum.Authorization.Rules;

namespace Signum.Tree;

public class TreeServer
{
    public static void Start(WebServerBuilder app)
    {
        if (app.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ReflectionServer.RegisterLike(typeof(TreeEntity), () => Schema.Current.Tables.Keys.Where(p => typeof(TreeEntity).IsAssignableFrom(p)).Any(t => TypeAuthLogic.GetAllowed(t).MaxUI() > TypeAllowedBasic.None));
    }
}
