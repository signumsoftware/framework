using Microsoft.AspNetCore.Builder;
using Signum.Engine.Authorization;
using Signum.Entities.Mailing;
using Signum.Entities.Templating;
using Signum.Entities.Word;
using Signum.React.Facades;

namespace Signum.React.Extensions.Templating;

public static class TemplatingServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        ReflectionServer.RegisterLike(typeof(TemplateTokenMessage), () =>
            TypeAuthLogic.GetAllowed(typeof(EmailTemplateEntity)).MaxUI() > Entities.Authorization.TypeAllowedBasic.None ||
            TypeAuthLogic.GetAllowed(typeof(WordTemplateEntity)).MaxUI() > Entities.Authorization.TypeAllowedBasic.None);
    }
}
